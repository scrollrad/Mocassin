﻿using System.Collections.Generic;
using System.Linq;
using Mocassin.Framework.Operations;
using Mocassin.Mathematics.Comparer;
using Mocassin.Mathematics.ValueTypes;
using Mocassin.Model.Basic;
using Mocassin.Model.ModelProject;
using Mocassin.Model.Structures;

namespace Mocassin.Model.Energies.ConflictHandling
{
    /// <summary>
    ///     Conflict resolver for a change in the stable environment info parameter within the energy manager
    /// </summary>
    public class StableEnvironmentInfoChangeHandler : ObjectConflictHandler<StableEnvironmentInfo, EnergyModelData>
    {
        /// <inheritdoc />
        public StableEnvironmentInfoChangeHandler(IDataAccessor<EnergyModelData> dataAccessor, IModelProject modelProject)
            : base(dataAccessor, modelProject)
        {
        }

        /// <inheritdoc />
        public override ConflictReport HandleConflicts(StableEnvironmentInfo obj)
        {
            var report = new ConflictReport();
            UpdatePairInteractionModel(obj, report);
            UpdateGroupInteractions(obj, report);
            return report;
        }

        /// <summary>
        ///     Updates the pair interaction model to the new set of pair interactions and inputs them into the model data object
        /// </summary>
        /// <param name="info"></param>
        /// <param name="report"></param>
        protected void UpdatePairInteractionModel(IStableEnvironmentInfo info, ConflictReport report)
        {
            var newPairs = GetNewPairInteractions(info);
            var oldPairs = DataAccess.Query(data => data.StablePairInteractions);

            PullEnergyInfoFromOldModel(oldPairs, newPairs, report, ModelProject.GeometryNumeric.RangeComparer);
            MoveNewPairsToModelList(oldPairs, newPairs, report);
        }

        /// <summary>
        ///     Finds all interactions that are equivalent in the new model and corrects the energy dictionaries to the ones of the
        ///     old model list
        /// </summary>
        /// <param name="oldPairs"></param>
        /// <param name="newPairs"></param>
        /// <param name="report"></param>
        /// <param name="comparer"></param>
        protected void PullEnergyInfoFromOldModel(IList<StablePairInteraction> oldPairs, IList<StablePairInteraction> newPairs,
            ConflictReport report, NumericComparer comparer)
        {
            var vectorComparer = new VectorComparer3D<Fractional3D>(comparer);
            var warning = ModelMessageSource.CreateConflictHandlingWarning(this);
            foreach (var oldPair in oldPairs)
            {
                switch (newPairs.FirstOrDefault(value => IsEquivalentInteraction(value, oldPair, vectorComparer)))
                {
                    case { } newPair:
                        newPair.EnergyDictionary = oldPair.EnergyDictionary;

                        var detail = $"Reused energy definition from pair ({oldPair.Index}) in new pair ({newPair.Index})";
                        warning.AddDetails(detail);
                        break;
                }
            }

            if (warning.Details.Count == 0)
                return;

            warning.AddDetails($"Recycled ({warning.Details.Count}) of ({newPairs.Count}) energy definitions");
            report.AddWarning(warning);
        }

        /// <summary>
        ///     Deletes the old model list and inputs the new pairs into the old model list without changing the actual list in the
        ///     model object
        /// </summary>
        /// <param name="oldPairs"></param>
        /// <param name="newPairs"></param>
        /// <param name="report"></param>
        protected void MoveNewPairsToModelList(IList<StablePairInteraction> oldPairs, IList<StablePairInteraction> newPairs,
            ConflictReport report)
        {
            if (oldPairs.Count != newPairs.Count)
            {
                var chiralPairCount = newPairs.Count(x => x.IsChiral);
                var uniquePairCount = newPairs.Count - chiralPairCount / 2;
                var detail0 = $"The pair interaction set was updated from ({oldPairs.Count}) to ({newPairs.Count}) interactions";
                var detail1 = $"Chiral behavior was detected in ({chiralPairCount}) cases, yielding ({uniquePairCount}) unique pairs.";
                report.AddWarning(ModelMessageSource.CreateConflictHandlingWarning(this, detail0, detail1));
            }

            oldPairs.Clear();
            foreach (var item in newPairs) oldPairs.Add(item);
        }

        /// <summary>
        ///     Checks if two pair interactions are equivalent in terms of symmetry. This function uses the fact that the radial
        ///     search routine always results
        ///     in the same reference pair interaction as long as the structure is not changed and only works as long as this
        ///     statement is true
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        protected bool IsEquivalentInteraction(StablePairInteraction lhs, StablePairInteraction rhs,
            VectorComparer3D<Fractional3D> comparer) =>
            lhs.Position0 == rhs.Position0
            && lhs.Position1 == rhs.Position1
            && comparer.Equals(lhs.SecondPositionVector, rhs.SecondPositionVector);

        /// <summary>
        ///     Calculates the new set of pair interactions with the provided project services and new stable environment info
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        protected IList<StablePairInteraction> GetNewPairInteractions(IStableEnvironmentInfo info)
        {
            var structureQueries = ModelProject.Manager<IStructureManager>().DataAccess;

            var unitCellProvider = structureQueries.Query(port => port.GetFullUnitCellProvider());
            var positions = structureQueries.Query(port => port.GetCellReferencePositions().Where(value => value.IsValidAndStable()));
            var comparer = ModelProject.GeometryNumeric.RangeComparer;

            var interactionFinder = new PairInteractionFinder(unitCellProvider, ModelProject.SpaceGroupService);
            return interactionFinder.SampleUniqueStablePairs(positions, info, comparer).ToList();
        }

        /// <summary>
        ///     Updates possible conflicts with the group interaction definitions (Currently just sets all stable group definitions
        ///     to deprecated)
        /// </summary>
        /// <param name="report"></param>
        /// <param name="info"></param>
        protected void UpdateGroupInteractions(IStableEnvironmentInfo info, ConflictReport report)
        {
            var counter = 0;
            foreach (var item in DataAccess.Query(data => data.GroupInteractions))
            {
                if (item.CenterCellSite.Stability != PositionStability.Stable)
                    continue;

                counter++;
                item.Deprecate();
            }

            if (counter == 0)
                return;

            const string detail0 = "Recovery of group interaction definitions on stable environment changes is currently not supported";
            const string detail1 = "Deprecated all group interactions affiliated with the changed unstable environment to avoid conflicts";
            report.AddWarning(ModelMessageSource.CreateContentResetWarning(this, detail0, detail1));
        }
    }
}