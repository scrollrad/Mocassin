﻿using Mocassin.Mathematics.ValueTypes;
using Mocassin.Model.Structures;

namespace Mocassin.Model.Translator.ModelContext
{
    /// <summary>
    ///     Carries information about a target position and its affiliated <see cref="IPairInteractionModel" />
    /// </summary>
    public interface ITargetPositionInfo
    {
        /// <summary>
        ///     Get or set the affiliated <see cref="IPairInteractionModel" />
        /// </summary>
        IPairInteractionModel PairInteractionModel { get; set; }

        /// <summary>
        ///     The unit cell position at the target
        /// </summary>
        ICellSite CellSite { get; set; }

        /// <summary>
        ///     The distance to the target in internal units
        /// </summary>
        double Distance { get; set; }

        /// <summary>
        ///     Absolute fractional vector of the target
        /// </summary>
        Fractional3D AbsoluteFractional { get; set; }

        /// <summary>
        ///     Relative fractional vector to the target
        /// </summary>
        Fractional3D RelativeFractional { get; set; }

        /// <summary>
        ///     Absolute cartesian vector of the target
        /// </summary>
        Cartesian3D AbsoluteCartesian { get; set; }

        /// <summary>
        ///     Relative crystal vector to the target
        /// </summary>
        Vector4I RelativeCrystalVector { get; set; }
    }
}