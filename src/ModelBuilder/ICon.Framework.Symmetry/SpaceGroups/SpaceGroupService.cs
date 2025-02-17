﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Mocassin.Framework.Collections;
using Mocassin.Framework.Comparer;
using Mocassin.Framework.Exceptions;
using Mocassin.Framework.Extensions;
using Mocassin.Framework.SQLiteCore;
using Mocassin.Mathematics.Comparer;
using Mocassin.Mathematics.Extensions;
using Mocassin.Mathematics.ValueTypes;
using Mocassin.Symmetry.CrystalSystems;

namespace Mocassin.Symmetry.SpaceGroups
{
    /// <inheritdoc />
    public class SpaceGroupService : ISpaceGroupService
    {
        /// <summary>
        ///     The space group context provider
        /// </summary>
        public ISqLiteContextSource<SpaceGroupContext> ContextSource { get; }

        /// <inheritdoc />
        public bool HasDbConnection => ContextSource != null;

        /// <inheritdoc />
        public ISpaceGroup LoadedGroup { get; protected set; }

        /// <summary>
        ///     The equality comparator which contains the almost equal information for the double vectors
        /// </summary>
        public VectorComparer3D<Fractional3D> VectorComparer { get; }

        /// <inheritdoc />
        public IComparer<Fractional3D> Comparer => VectorComparer;

        /// <summary>
        ///     Get the <see cref="IComparer{T}" /> for double values used by the vector comparer
        /// </summary>
        public IComparer<double> DoubleComparer => VectorComparer.ValueComparer;

        /// <summary>
        ///     Creates new <see cref="SpaceGroupService" /> that uses the provided comparer and database file path
        /// </summary>
        /// <param name="dbFilepath"></param>
        /// <param name="comparer"></param>
        public SpaceGroupService(string dbFilepath, IComparer<double> comparer)
            : this(comparer)
        {
            if (dbFilepath == null) throw new ArgumentNullException(nameof(dbFilepath));
            ContextSource = new SpaceGroupContextSource(dbFilepath);
        }

        /// <summary>
        ///     Creates a new <see cref="SpaceGroupService" /> with the passed comparer that does not have a database connection
        /// </summary>
        /// <param name="comparer"></param>
        public SpaceGroupService(IComparer<double> comparer)
        {
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));
            VectorComparer = new VectorComparer3D<Fractional3D>(comparer);
        }

        /// <summary>
        ///     Creates new space group service using the default space group context, database path and tolerance value for
        ///     comparisons
        /// </summary>
        /// <param name="dbFilepath"></param>
        /// <param name="tolerance"></param>
        public SpaceGroupService(string dbFilepath, double tolerance)
            : this(dbFilepath, NumericComparer.CreateRanged(tolerance))
        {
            TryLoadGroup(group => group.InternationalIndex == 1);
        }

        /// <summary>
        ///     Creates new space group service from custom SQLiteCoreContextProvider
        /// </summary>
        /// <param name="contextSource"></param>
        /// <param name="comparer"></param>
        public SpaceGroupService(ISqLiteContextSource<SpaceGroupContext> contextSource, IComparer<double> comparer)
        {
            ContextSource = contextSource ?? throw new ArgumentNullException(nameof(contextSource));
            VectorComparer = new VectorComparer3D<Fractional3D>(comparer);
        }

        /// <inheritdoc />
        public bool TryLoadGroup(Expression<Func<SpaceGroupEntity, bool>> searchPredicate)
        {
            if (!HasDbConnection) return false;

            SpaceGroupEntity newGroup;
            using (var context = ContextSource.CreateContext())
            {
                newGroup = context.SpaceGroups
                                  .Where(searchPredicate)
                                  .Include(g => g.BaseSymmetryOperations)
                                  .SingleOrDefault();

                if (LoadedGroup == null) LoadedGroup = newGroup;

                if (newGroup != null && !newGroup.GetGroupEntry().Equals(LoadedGroup.GetGroupEntry()))
                {
                    LoadedGroup = !newGroup.GetGroupEntry().Equals(LoadedGroup.GetGroupEntry())
                        ? newGroup
                        : LoadedGroup;
                }
            }

            return newGroup != null;
        }

        /// <inheritdoc />
        public bool TryLoadGroup(SpaceGroupEntry groupEntry)
        {
            if (!HasDbConnection) return false;
            if (groupEntry == null) throw new ArgumentNullException(nameof(groupEntry));
            if (LoadedGroup != null && groupEntry.Equals(LoadedGroup.GetGroupEntry())) return true;
            return TryLoadGroup(group => group.InternationalIndex == groupEntry.GroupNumber && group.CrystalVariation == groupEntry.CrystalVariation && groupEntry.VariationName == group.VariationName);
        }

        /// <inheritdoc />
        public void LoadGroup(ISpaceGroup spaceGroup)
        {
            if (spaceGroup == null) throw new ArgumentNullException(nameof(spaceGroup));
            if (!HasDbConnection)
            {
                if (LoadedGroup != spaceGroup) LoadedGroup = spaceGroup;
                return;
            }

            if (!TryLoadGroup(new SpaceGroupEntry(spaceGroup)))
                throw new InvalidOperationException("The services has a DB context which does not contain the requested group!");
        }

        /// <inheritdoc />
        public CrystalSystem CreateCrystalSystem(ICrystalSystemSource source)
        {
            if (LoadedGroup == null)
                throw new InvalidObjectStateException("No space group loaded, cannot create crystal system");

            return source.GetSystem(LoadedGroup);
        }

        /// <inheritdoc />
        public SetList<ISpaceGroup> GetFullGroupList()
        {
            var setList = new SetList<ISpaceGroup>(Comparer<ISpaceGroup>.Default, 274);
            using (var context = ContextSource.CreateContext())
            {
                setList.AddRange(context.SpaceGroups.Include(x => x.BaseSymmetryOperations));
            }

            return setList;
        }

        /// <inheritdoc />
        public SetList<Fractional3D> GetUnitCellP1PositionExtension(in Fractional3D refVector) => CreatePositionSetList(refVector);

        /// <inheritdoc />
        public List<SetList<Fractional3D>> GetUnitCellP1PositionExtensions(IEnumerable<Fractional3D> refVectors)
        {
            if (refVectors == null)
                throw new ArgumentNullException(nameof(refVectors));

            return refVectors.Select(GetUnitCellP1PositionExtension).ToList();
        }

        /// <inheritdoc />
        public SetList<Fractional3D> GetUnitCellP1PositionExtension<TSource>(TSource refVector) where TSource : IFractional3D =>
            CreatePositionSetList(new Fractional3D(refVector.Coordinates));

        /// <inheritdoc />
        public List<SetList<Fractional3D>> GetUnitCellP1PositionExtensions<TSource>(IEnumerable<TSource> refVectors)
            where TSource : IFractional3D
        {
            if (refVectors == null)
                throw new ArgumentNullException(nameof(refVectors));

            return refVectors.Select(GetUnitCellP1PositionExtension).ToList();
        }

        /// <inheritdoc />
        public IList<Fractional3D[]> GetFullP1PathExtension(IEnumerable<Fractional3D> refSequence)
        {
            var refCollection = refSequence.AsCollection();
            return LoadedGroup.Operations
                              .Select(operation => refCollection.Select(vector => operation.Transform(vector)).ToArray(refCollection.Count))
                              .ToList(LoadedGroup.Operations.Count);
        }

        /// <inheritdoc />
        public SetList<Fractional3D[]> GetUnitCellP1PathExtension(IEnumerable<Fractional3D> refSequence)
        {
            var refCollection = refSequence.AsCollection();

            var comparer = Comparer<Fractional3D[]>.Create((a, b) => a.LexicographicCompare(b, VectorComparer));
            var list = new SetList<Fractional3D[]>(comparer)
            {
                GetFullP1PathExtension(refCollection).Select(sequence => ShiftFirstToOriginCell(sequence, DoubleComparer).ToArray(sequence.Length))
            };

            return list;
        }

        /// <inheritdoc />
        public IList<ISymmetryOperation> GetMinimalUnitCellP1PathExtensionOperations(IEnumerable<Fractional3D> refSequence,
            bool filterGeometricDuplicates = false)
        {
            var refVectors = refSequence.AsList();

            var operations = new List<ISymmetryOperation>(LoadedGroup.Operations.Count);
            operations.AddRange(LoadedGroup.Operations.Select(x =>
                GetOriginCellShiftedOperations(x.Transform(refVectors[0]), x, DoubleComparer)));

            var sequences = new SetList<IList<Fractional3D>>(
                Comparer<IList<Fractional3D>>.Create((a, b) => a.LexicographicCompare(b, VectorComparer)),
                operations.Count);
            var filteredOperations = new List<ISymmetryOperation>(LoadedGroup.Operations.Count / 2);

            var transformedVectors = new List<Fractional3D>(refVectors.Count);
            foreach (var operation in operations)
            {
                transformedVectors.Clear();
                transformedVectors.AddRange(operation.Transform(refVectors));
                if (sequences.Contains(transformedVectors)) continue;
                if (filterGeometricDuplicates && !CheckSequenceIsGeometricUnique(transformedVectors, sequences)) continue;
                var transformCopy = transformedVectors.ToList(transformedVectors.Count);
                sequences.Add(transformCopy);
                filteredOperations.Add(operation);
            }

            return filteredOperations;
        }

        /// <inheritdoc />
        public IEnumerable<Fractional3D> ShiftFirstToOriginCell(IEnumerable<Fractional3D> source, double tolerance)
        {
            var sourceList = source.AsList();

            var start = sourceList[0];
            var shift = start.TrimToUnitCell(tolerance) - start;
            return sourceList.Select(value => value + shift);
        }

        /// <inheritdoc />
        public IEnumerable<Fractional3D> ShiftFirstToOriginCell(IEnumerable<Fractional3D> source, IComparer<double> comparer)
        {
            var sourceList = source.AsList();

            var start = sourceList[0];
            var shift = start.TrimToUnitCell(comparer) - start;
            return sourceList.Select(value => value + shift);
        }

        /// <inheritdoc />
        public ISymmetryOperation GetOriginCellShiftedOperations(in Fractional3D start, ISymmetryOperation operation, double tolerance)
        {
            var shift = start.TrimToUnitCell(tolerance) - start;
            return GetTranslationShiftedOperation(operation, shift);
        }

        /// <inheritdoc />
        public ISymmetryOperation GetOriginCellShiftedOperations(in Fractional3D start, ISymmetryOperation operation, IComparer<double> comparer)
        {
            var shift = start.TrimToUnitCell(comparer) - start;
            return GetTranslationShiftedOperation(operation, shift);
        }

        /// <inheritdoc />
        public ISymmetryOperation GetOperationToTarget(in Fractional3D source, in Fractional3D target)
        {
            foreach (var operation in LoadedGroup.Operations)
            {
                var vector = operation.TrimTransform(source, out var trimVector);
                if (Comparer.Compare(vector, target) == 0)
                    return GetTranslationShiftedOperation(operation, trimVector);
            }

            return null;
        }

        /// <inheritdoc />
        public IList<Fractional3D> GetPositionsInCuboid(in Fractional3D source, in FractionalBox3D boundingBox)
        {
            var equalityComparer = VectorComparer.ValueComparer.ToEqualityComparer();
            var basePositions = GetUnitCellP1PositionExtension(source);
            var (start, end) = (boundingBox.Start, boundingBox.End);
            var (aMin, bMin, cMin) = (start.A.FloorToInt(equalityComparer), start.B.FloorToInt(equalityComparer), start.C.FloorToInt(equalityComparer));
            var (aMax, bMax, cMax) = (end.A.FloorToInt(equalityComparer), end.B.FloorToInt(equalityComparer), end.C.FloorToInt(equalityComparer));

            var cellCount = Math.Abs(aMax - aMin) * Math.Abs(bMax - bMin) * Math.Abs(cMax - cMin);
            var result = new List<Fractional3D>(cellCount * basePositions.Count);
            for (var a = aMin; a <= aMax; a++)
            {
                for (var b = bMin; b <= bMax; b++)
                {
                    for (var c = cMin; c <= cMax; c++)
                    {
                        foreach (var entry in basePositions)
                        {
                            var vector = new Fractional3D(entry.A + a, entry.B + b, entry.C + c);
                            if (!boundingBox.IsWithinBounds(vector, VectorComparer.ValueComparer)) continue;
                            result.Add(vector);
                        }
                    }
                }
            }

            return result;
        }

        /// <inheritdoc />
        public IList<Fractional3D> GetPositionsInCuboid(in Fractional3D source, in Fractional3D start, in Fractional3D end)
        {
            var boundingBox = new FractionalBox3D(start, end - start);
            return GetPositionsInCuboid(source, boundingBox);
        }

        /// <inheritdoc />
        public bool CheckInteractionGeometryIsChiralPair(in Fractional3D left0, in Fractional3D right0, in Fractional3D left1, in Fractional3D right1)
        {
            var leftSet = GetUnitCellP1PathExtension(new[] {left0, right0});
            var rightSet = GetUnitCellP1PathExtension(new[] {right0, left0});
            var testArray = new[] {left1, right1};
            return !leftSet.Contains(testArray) && rightSet.Contains(testArray);
        }

        /// <inheritdoc />
        public bool CheckInteractionGeometryIsChiral(in Fractional3D left, in Fractional3D right) =>
            CheckInteractionGeometryIsChiralPair(left, right, right, left);

        /// <inheritdoc />
        public IPositionOperationDictionary GetOperationDictionary(in Fractional3D sourceVector)
        {
            var operationComparer =
                Comparer<ISymmetryOperation>.Create((a, b) => string.Compare(a.Literal, b.Literal, StringComparison.Ordinal));
            var dictionary = new SortedDictionary<Fractional3D, SetList<ISymmetryOperation>>(VectorComparer);

            foreach (var operation in LoadedGroup.Operations)
            {
                var vector = operation.TrimTransform(sourceVector);
                if (!dictionary.ContainsKey(vector))
                    dictionary[vector] = new SetList<ISymmetryOperation>(operationComparer);

                dictionary[vector].Add(operation);
            }

            return new PositionOperationDictionary(sourceVector, LoadedGroup, dictionary);
        }

        /// <inheritdoc />
        public IList<ISymmetryOperation> GetOperationsNotShiftingOrigin(in Fractional3D sourceVector, bool useInvarianceCorrection)
        {
            var result = new List<ISymmetryOperation>(LoadedGroup.Operations.Count);
            foreach (var operation in LoadedGroup.Operations)
            {
                var untrimmedVector = operation.Transform(sourceVector);
                if (Comparer.Compare(untrimmedVector.TrimToUnitCell(operation.TrimTolerance), sourceVector) != 0)
                    continue;

                var shift = useInvarianceCorrection ? sourceVector - untrimmedVector : Fractional3D.Zero;
                var newOperation = GetTranslationShiftedOperation(operation, shift);
                result.Add(newOperation);
            }

            result.TrimExcess();
            return result;
        }

        /// <inheritdoc />
        public IPointOperationGroup GetPointOperationGroup(in Fractional3D originPoint, IEnumerable<Fractional3D> pointSequence)
        {
            var originShiftedData = ShiftFirstToOriginCell(originPoint.AsSingleton().Concat(pointSequence), DoubleComparer).ToList();

            var shiftedOriginPoint = originShiftedData[0];
            var pointList = originShiftedData.Skip(1).AsList();

            var operationGroup = new PointOperationGroup
            {
                SpaceGroupEntry = LoadedGroup.GetGroupEntry(),
                UniqueOriginSiteCount = GetUnitCellP1PositionExtension(in shiftedOriginPoint).Count,
                OriginPoint = shiftedOriginPoint,
                PointSequence = pointList.ToList(),
                OrderIgnoringSelfProjectionOperations = new List<SymmetryOperation>(),
                OrderIgnoringUniqueSequenceOperations = new List<SymmetryOperation>()
            };

            var operationsAtOrigin = GetOperationsNotShiftingOrigin(shiftedOriginPoint, true);
            var sequencesAtOrigin = operationsAtOrigin
                                    .Select(operation => operation.Transform(pointList).ToList())
                                    .ToList();

            operationGroup.LocalSequenceOperations = operationsAtOrigin.Cast<SymmetryOperation>().ToList();

            var equalityComparer = MakeVectorSequenceProjectionComparer();

            for (var i = 0; i < operationsAtOrigin.Count; i++)
            {
                var isAlreadyDefined = false;
                for (var j = 0; j < i; j++)
                {
                    if (!equalityComparer.Equals(sequencesAtOrigin[i], sequencesAtOrigin[j])) continue;
                    isAlreadyDefined = true;
                    break;
                }

                if (!isAlreadyDefined) operationGroup.OrderIgnoringUniqueSequenceOperations.Add((SymmetryOperation) operationsAtOrigin[i]);
            }

            foreach (var index in sequencesAtOrigin.IndexOfMany(value => equalityComparer.Equals(value, sequencesAtOrigin.First())))
                operationGroup.OrderIgnoringSelfProjectionOperations.Add((SymmetryOperation) operationsAtOrigin[index]);

            foreach (var index in sequencesAtOrigin.RemoveDuplicatesAndGetRemovedIndices(MakeVectorSequenceEquivalenceComparer()))
                operationsAtOrigin.RemoveAt(index);

            operationGroup.OrderPreservingUniqueSequenceOperations = operationsAtOrigin.Cast<SymmetryOperation>().ToList();
            operationGroup.SelfProjectionOrders = CalculateProjectionOrderMatrix(pointList, operationGroup.OrderIgnoringSelfProjectionOperations);
            return operationGroup;
        }

        /// <inheritdoc />
        public SortedDictionary<Fractional3D, List<Fractional3D>> GetEnvironmentDictionary(IEnumerable<Fractional3D> refSequence)
        {
            var refList = refSequence.AsList();

            var result = new SortedDictionary<Fractional3D, List<Fractional3D>>(VectorComparer);
            var operationDictionary = GetOperationDictionary(refList[0]);

            foreach (var entry in operationDictionary)
            {
                var operation = entry.Value.ElementAt(0);
                var sequence = ShiftFirstToPosition(refList.Select(value => operation.Transform(value)), entry.Key);
                result.Add(entry.Key, sequence.ToList());
            }

            return result;
        }

        /// <inheritdoc />
        public IComparer<T1> GetSpecialVectorComparer<T1>() where T1 : IVector3D => new VectorComparer3D<T1>(DoubleComparer);

        /// <summary>
        ///     Checks if a sequence of <see cref="Fractional3D" /> does not describe a sequence in the options set in any order
        /// </summary>
        /// <param name="sequence"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private bool CheckSequenceIsGeometricUnique(IList<Fractional3D> sequence, IList<IList<Fractional3D>> options)
        {
            return !options.Any(x => CheckSameGeometryIfOrderIgnored(x, sequence));
        }

        /// <summary>
        ///     Checks if two sequences of <see cref="Fractional3D" /> positions can from the same geometry if the order is
        ///     ignored. This function only works if both collections do not contain duplicates
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        private bool CheckSameGeometryIfOrderIgnored(ICollection<Fractional3D> lhs, ICollection<Fractional3D> rhs)
        {
            if (rhs.Count != lhs.Count) return false;

            foreach (var lhsItem in lhs)
            {
                var itemMatchCount = rhs.Count(rhsItem => VectorComparer.Equals(rhsItem, lhsItem));
                if (itemMatchCount == 0)
                    return false;
                if (itemMatchCount != 1)
                    throw new InvalidOperationException("This function does not support sets with position duplicates.");
            }

            return true;
        }

        /// <summary>
        ///     Creates the position set list of all equivalent vectors to the passed source
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        protected SetList<Fractional3D> CreatePositionSetList(Fractional3D vector)
        {
            var results = new SetList<Fractional3D>(VectorComparer, LoadedGroup.Operations.Count);
            foreach (var operation in LoadedGroup.Operations)
                results.Add(operation.TrimTransform(vector));

            results.List.TrimExcess();
            return results;
        }

        /// <summary>
        ///     Creates a basic fractional position set list from double coordinate values
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        protected SetList<Fractional3D> CreatePositionSetList(double a, double b, double c)
        {
            var results = new SetList<Fractional3D>(VectorComparer, LoadedGroup.Operations.Count);
            foreach (var operation in LoadedGroup.Operations)
                results.Add(operation.TrimTransform(a, b, c));

            results.List.TrimExcess();
            return results;
        }

        /// <summary>
        ///     Creates a source type fractional position set list where all results carry the original information of the source
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="vector"></param>
        /// <returns></returns>
        protected SetList<Fractional3D> CreatePositionSetList<TSource>(TSource vector) where TSource : IFractional3D
        {
            var results = new SetList<Fractional3D>(VectorComparer, LoadedGroup.Operations.Count);

            foreach (var operation in LoadedGroup.Operations)
                results.Add(operation.TrimTransform(vector));

            results.List.TrimExcess();
            return results;
        }

        /// <summary>
        ///     Shifts all positions so that the resulting sequence first position is on the new position
        /// </summary>
        /// <param name="source"></param>
        /// <param name="newFirst"></param>
        /// <returns></returns>
        public IEnumerable<Fractional3D> ShiftFirstToPosition(IEnumerable<Fractional3D> source, in Fractional3D newFirst)
        {
            var sourceList = source.AsList();

            var shift = newFirst - sourceList[0];
            return sourceList.Select(value => value + shift);
        }

        /// <summary>
        ///     Add a translation shift to a symmetry operation and returns a new symmetry operation that contains the new
        ///     operations array and literal description
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="shift"></param>
        /// <returns></returns>
        public ISymmetryOperation GetTranslationShiftedOperation(ISymmetryOperation operation, in Fractional3D shift)
        {
            var shifted = SymmetryOperation.CreateShifted(operation, shift);
            shifted.Literal = $"({operation.Literal}) + ({shift.A} ,{shift.B}, {shift.C})";
            shifted.TrimTolerance = operation.TrimTolerance;
            return shifted;
        }

        /// <summary>
        ///     Determines the unique possible projection orders of the positions within the passed vector sequence and operation
        ///     set
        /// </summary>
        /// <param name="vectors"></param>
        /// <param name="symmetryOperations"></param>
        /// <returns> Operations have to be self projection operations for this function to yield meaningful results </returns>
        protected List<List<int>> CalculateProjectionOrderMatrix(IEnumerable<Fractional3D> vectors,
            IEnumerable<ISymmetryOperation> symmetryOperations)
        {
            var options = symmetryOperations.Select(operation => operation.Transform(vectors).ToList()).ToList();
            var uniqueIndexSets = new HashSet<List<int>>(new RelayEqualityComparer<List<int>>((a, b) => a.SequenceEqual(b), a => a.Sum()));

            foreach (var option in options)
            {
                var currentIndexing = new List<int>(8);
                foreach (var vector in option)
                    currentIndexing.Add(options[0].FindIndex(0, value => VectorComparer.Equals(value, vector)));

                uniqueIndexSets.Add(currentIndexing);
            }

            return uniqueIndexSets.ToList();
        }

        /// <summary>
        ///     Creates a geometry sequence comparer that returns true if two sequences are equivalent or reversing one sequence
        ///     makes them equivalent
        /// </summary>
        /// <returns></returns>
        protected IEqualityComparer<IList<Fractional3D>> MakeVectorSequenceEquivalenceComparer()
        {
            bool AreEquivalent(IList<Fractional3D> lhs, IList<Fractional3D> rhs) => lhs.GetSequenceEqualityDirectionTo(rhs, VectorComparer) != 0;

            return new RelayEqualityComparer<IList<Fractional3D>>(AreEquivalent);
        }

        /// <summary>
        ///     Creates a geometry sequence comparer that returns true if two sequences contain the same set of vectors in any
        ///     order
        /// </summary>
        /// <returns></returns>
        protected IEqualityComparer<IList<Fractional3D>> MakeVectorSequenceProjectionComparer()
        {
            bool AreEquivalent(IList<Fractional3D> lhs, IList<Fractional3D> rhs)
            {
                return lhs.Count == rhs.Count && lhs.Select(value => rhs.Contains(value, VectorComparer)).All(value => value);
            }

            return new RelayEqualityComparer<IList<Fractional3D>>(AreEquivalent);
        }
    }
}