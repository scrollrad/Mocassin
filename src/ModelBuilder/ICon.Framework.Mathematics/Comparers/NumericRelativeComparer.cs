﻿using System;
using Mocassin.Mathematics.Extensions;

namespace Mocassin.Mathematics.Comparer
{
    /// <summary>
    ///     Double relative comparer class that performs almost equal comparisons with a relative factor (Not zero safe)
    /// </summary>
    public class NumericRelativeComparer : NumericComparer
    {
        /// <summary>
        ///     Shared zero safe flag
        /// </summary>
        private static readonly bool ZeroSaveValue = false;

        /// <summary>
        ///     The absolute tolerance range value
        /// </summary>
        public double RelativeRange { get; protected set; }

        /// <inheritdoc />
        public override bool IsZeroCompatible => ZeroSaveValue;

        /// <summary>
        ///     Creates a new relative range comparer with the specified tolerance factor
        /// </summary>
        /// <param name="range"></param>
        public NumericRelativeComparer(double range)
        {
            RelativeRange = Math.Abs(range);
        }

        /// <inheritdoc />
        public override int Compare(double lhs, double rhs) => Equals(lhs, rhs) ? 0 : lhs < rhs ? -1 : 1;

        /// <inheritdoc />
        public override bool Equals(double x, double y) => x.AlmostEqualByFactor(y, RelativeRange);
    }
}