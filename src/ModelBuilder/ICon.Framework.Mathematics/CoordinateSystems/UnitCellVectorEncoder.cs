﻿using System;
using System.Collections.Generic;
using Mocassin.Framework.Collections;
using Mocassin.Mathematics.Extensions;
using Mocassin.Mathematics.ValueTypes;

namespace Mocassin.Mathematics.Coordinates
{
    /// <inheritdoc />
    public class UnitCellVectorEncoder : IUnitCellVectorEncoder
    {
        /// <inheritdoc />
        public SetList<Fractional3D> PositionList { get; }

        /// <inheritdoc />
        public IVectorTransformer Transformer { get; }

        /// <inheritdoc />
        public int PositionCount => PositionList.Count;

        /// <summary>
        ///     Creates new vector encoder with specified position list and vector transformer
        /// </summary>
        /// <param name="positionList"></param>
        /// <param name="vectorTransformer"></param>
        public UnitCellVectorEncoder(SetList<Fractional3D> positionList, IVectorTransformer vectorTransformer)
        {
            PositionList = positionList ?? throw new ArgumentNullException(nameof(positionList));
            Transformer = vectorTransformer ?? throw new ArgumentNullException(nameof(vectorTransformer));
        }


        /// <inheritdoc />
        public bool TryEncode(in Fractional3D vector, out Vector4I encoded) => TryEncodeFractional(vector.Coordinates, out encoded);

        /// <inheritdoc />
        public bool TryEncode(in Fractional3D origin, in Fractional3D vector, out Vector4I encoded) =>
            TryEncode(new Fractional3D(vector.A + origin.A, vector.B + origin.B, vector.C + origin.C), out encoded);

        /// <inheritdoc />
        public bool TryEncode(IEnumerable<Fractional3D> decoded, out List<Vector4I> encoded)
        {
            encoded = new List<Vector4I>(100);
            foreach (var item in decoded)
            {
                if (!TryEncode(item, out var singleEncoded)) return false;
                encoded.Add(singleEncoded);
            }

            return true;
        }

        /// <inheritdoc />
        public bool TryEncodeAsRelative(in Fractional3D origin, in Fractional3D vector, out Vector4I encoded)
        {
            var startSuccess = TryEncode(origin, out var encodedStart);
            var endSuccess = TryEncode(origin, vector, out var encodedEnd);
            if (startSuccess && endSuccess)
            {
                encoded = encodedEnd - encodedStart;
                return true;
            }

            encoded = default;
            return false;
        }

        /// <inheritdoc />
        public bool TryEncode(in Cartesian3D vector, out Vector4I encoded) => TryEncode(Transformer.ToFractional(vector), out encoded);

        /// <inheritdoc />
        public bool TryEncode(in Cartesian3D origin, in Cartesian3D vector, out Vector4I encoded) =>
            TryEncode(Transformer.ToFractional(origin), Transformer.ToFractional(vector), out encoded);

        /// <inheritdoc />
        public bool TryEncodeAsRelative(in Cartesian3D origin, in Cartesian3D vector, out Vector4I encoded) =>
            TryEncodeAsRelative(Transformer.ToFractional(origin), Transformer.ToFractional(vector), out encoded);

        /// <inheritdoc />
        public bool TryEncode(in Spherical3D vector, out Vector4I encoded) => TryEncode(Transformer.ToFractional(vector), out encoded);

        /// <inheritdoc />
        public bool TryDecode(in Vector4I encoded, out Fractional3D decoded) => TryDecodeFractional(encoded.Coordinates, out decoded);

        /// <inheritdoc />
        public void DecodeUnchecked(in Vector4I encoded, out Fractional3D decoded)
        {
            var offset = PositionList[encoded.P];
            decoded = new Fractional3D(encoded.A + offset.A, encoded.B + offset.B, encoded.C + offset.C);
        }

        /// <inheritdoc />
        public bool TryDecode(IEnumerable<Vector4I> encoded, out List<Fractional3D> decoded)
        {
            var result = new List<Fractional3D>();
            foreach (var item in encoded)
            {
                if (!TryDecode(item, out Fractional3D itemDecoded))
                {
                    decoded = default;
                    return false;
                }

                result.Add(itemDecoded);
            }

            decoded = result;
            return true;
        }

        /// <inheritdoc />
        public bool TryDecode(in Vector4I encoded, out Cartesian3D decoded)
        {
            if (!TryDecode(encoded, out Fractional3D fractional))
            {
                decoded = default;
                return false;
            }

            decoded = Transformer.ToCartesian(fractional);
            return true;
        }

        /// <inheritdoc />
        public void DecodeUnchecked(in Vector4I encoded, out Cartesian3D decoded)
        {
            DecodeUnchecked(encoded, out Fractional3D fractional3D);
            decoded = Transformer.ToCartesian(fractional3D);
        }

        /// <inheritdoc />
        public bool TryDecode(in Vector4I encoded, out Spherical3D decoded)
        {
            if (!TryDecode(encoded, out Fractional3D fractional))
            {
                decoded = default;
                return false;
            }

            decoded = Transformer.ToSpherical(fractional);
            return true;
        }

        /// <inheritdoc />
        public bool TryDecodeToRelative(in Vector4I origin, in Vector4I encoded, out Fractional3D decoded)
        {
            var total = new Vector4I(origin.A + encoded.A, origin.B + encoded.B, origin.C + encoded.C, origin.P + encoded.P);
            var totalSuccess = TryDecode(total, out Fractional3D totalDecoded);
            var originSuccess = TryDecode(origin, out Fractional3D originDecoded);
            if (totalSuccess && originSuccess)
            {
                decoded = totalDecoded - originDecoded;
                return true;
            }

            decoded = default;
            return false;
        }

        /// <inheritdoc />
        public bool TryDecodeToRelative(in Vector4I origin, in Vector4I encoded, out Cartesian3D decoded)
        {
            var total = new Vector4I(origin.A + encoded.A, origin.B + encoded.B, origin.C + encoded.C, origin.P + encoded.P);
            var totalSuccess = TryDecode(total, out Cartesian3D totalDecoded);
            var originSuccess = TryDecode(origin, out Cartesian3D originDecoded);
            if (totalSuccess && originSuccess)
            {
                decoded = totalDecoded - originDecoded;
                return true;
            }

            decoded = default;
            return false;
        }

        /// <inheritdoc />
        public bool TryDecodeToRelative(in Vector4I origin, in Vector4I encoded, out Spherical3D decoded)
        {
            if (!TryDecodeToRelative(origin, encoded, out Cartesian3D decodedCartesian))
            {
                decoded = default;
                return false;
            }

            decoded = Transformer.ToSpherical(decodedCartesian);
            return true;
        }

        /// <inheritdoc />
        public VectorI3 GetTargetCellOffset(in Fractional3D vector) => GetTargetCellOffset(vector.Coordinates);

        /// <inheritdoc />
        public Fractional3D GetOriginCellTrimmedVector(in Fractional3D vector, out VectorI3 offset) =>
            GetOriginCellTrimmedVector(vector.Coordinates, out offset);

        /// <inheritdoc />
        public (Cartesian3D A, Cartesian3D B, Cartesian3D C) GetBaseVectors() => Transformer.FractionalSystem.GetBaseVectors();

        /// <inheritdoc />
        public double GetCellVolume()
        {
            var (a, b, c) = GetBaseVectors();
            return Math.Abs(a.GetSpatProduct(b, c));
        }

        /// <inheritdoc />
        public Fractional3D GetPosition(int index) => PositionList[index];

        /// <inheritdoc />
        public Cartesian3D GetCartesianPosition(int index) => Transformer.ToCartesian(PositionList[index]);

        /// <summary>
        ///     Internal fractional encode function that uses a <see cref="Coordinates3D" /> input
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="encoded"></param>
        /// <returns></returns>
        private bool TryEncodeFractional(in Coordinates3D vector, out Vector4I encoded)
        {
            var index = PositionList.IndexOf(GetOriginCellTrimmedVector(vector, out var offset));
            if (index < 0)
            {
                encoded = default;
                return false;
            }

            encoded = new Vector4I(offset.A, offset.B, offset.C, index);
            return true;
        }

        /// <summary>
        ///     Internal decode from <see cref="Coordinates4I" /> to <see cref="Fractional3D" />
        /// </summary>
        /// <param name="encoded"></param>
        /// <param name="decoded"></param>
        /// <returns></returns>
        private bool TryDecodeFractional(in Coordinates4I encoded, out Fractional3D decoded)
        {
            if (encoded.D < 0 || encoded.D >= PositionList.Count)
            {
                decoded = default;
                return false;
            }

            var offset = PositionList[encoded.D];
            decoded = new Fractional3D(encoded.A + offset.A, encoded.B + offset.B, encoded.C + offset.C);
            return true;
        }

        /// <summary>
        ///     Calculates the target cell offset from a <see cref="Coordinates3D" />
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        private VectorI3 GetTargetCellOffset(in Coordinates3D vector)
        {
            var a = vector.A.FloorToInt(Transformer.FractionalSystem.Comparer);
            var b = vector.B.FloorToInt(Transformer.FractionalSystem.Comparer);
            var c = vector.C.FloorToInt(Transformer.FractionalSystem.Comparer);
            return new VectorI3(a, b, c);
        }

        /// <summary>
        ///     Calculates an origin cell trimmed vector from a <see cref="Coordinates3D" />
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private Fractional3D GetOriginCellTrimmedVector(in Coordinates3D vector, out VectorI3 offset)
        {
            offset = GetTargetCellOffset(vector);
            return new Fractional3D(vector.A - offset.A, vector.B - offset.B, vector.C - offset.C);
        }
    }
}