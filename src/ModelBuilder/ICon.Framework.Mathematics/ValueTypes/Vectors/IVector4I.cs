﻿namespace Mocassin.Mathematics.ValueTypes
{
    /// <summary>
    ///     General interface for all four dimensional 128 bit encoded linear supercell crystal position information
    /// </summary>
    public interface IVector4I
    {
        /// <summary>
        ///     Get the coordinate tuple
        /// </summary>
        Coordinates4I Coordinates { get; }

        /// <summary>
        ///     Offset in A direction
        /// </summary>
        int A { get; }

        /// <summary>
        ///     Offset in B direction
        /// </summary>
        int B { get; }

        /// <summary>
        ///     Offset in C direction
        /// </summary>
        int C { get; }

        /// <summary>
        ///     The position ID within the unit cell
        /// </summary>
        int P { get; }
    }
}