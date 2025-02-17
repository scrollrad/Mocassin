﻿using System.Collections.Generic;
using Mocassin.Model.Particles;
using Mocassin.Model.Structures;
using Mocassin.Symmetry.SpaceGroups;

namespace Mocassin.Model.Energies
{
    /// <summary>
    ///     Represents an extended position group that carries all information to fully describe the properties of a group
    ///     interaction
    /// </summary>
    public class ExtendedPositionGroup
    {
        /// <summary>
        ///     The group interaction the extended position group was created from
        /// </summary>
        public IGroupInteraction GroupInteraction { get; set; }

        /// <summary>
        ///     The center position
        /// </summary>
        public ICellSite CenterPosition { get; set; }

        /// <summary>
        ///     The unit cell positions of the group sequences
        /// </summary>
        public List<ICellSite> SurroundingCellReferencePositions { get; set; }

        /// <summary>
        ///     The point operation group describing the symmetry operation information of the group
        /// </summary>
        public IPointOperationGroup PointOperationGroup { get; set; }

        /// <summary>
        ///     List of all unique occupation states without the center position
        /// </summary>
        public List<OccupationState> UniqueOccupationStates { get; set; }

        /// <summary>
        ///     The unique energy dictionary for each unique occupation state around each unique center particle
        /// </summary>
        public Dictionary<IParticle, Dictionary<OccupationState, double>> UniqueEnergyDictionary { get; set; }
    }
}