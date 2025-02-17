﻿using System;
using System.Xml.Serialization;
using Mocassin.Model.Energies;
using Mocassin.Model.Particles;
using Mocassin.Model.Structures;
using Mocassin.UI.Data.Base;

namespace Mocassin.UI.Data.EnergyModel
{
    /// <summary>
    ///     Serializable data object for storing defect energies
    /// </summary>
    [XmlRoot]
    public class DefectEnergyData : ProjectDataObject, IComparable<DefectEnergyData>
    {
        private ModelObjectReference<CellSite> cellReferencePosition;
        private double energy;
        private ModelObjectReference<Particle> particle;

        /// <summary>
        ///     Get or set the <see cref="ModelObjectReference{T}" /> for the defect <see cref="Particle" />
        /// </summary>
        [XmlElement]
        public ModelObjectReference<Particle> Particle
        {
            get => particle;
            set => SetProperty(ref particle, value);
        }

        /// <summary>
        ///     Get or set the <see cref="ModelObjectReference{T}" /> for the defect <see cref="CellReferencePosition" />
        /// </summary>
        [XmlElement]
        public ModelObjectReference<CellSite> CellReferencePosition
        {
            get => cellReferencePosition;
            set => SetProperty(ref cellReferencePosition, value);
        }

        /// <summary>
        ///     Get or set the energy value in [eV]
        /// </summary>
        [XmlAttribute]
        public double Energy
        {
            get => energy;
            set => SetProperty(ref energy, value);
        }

        /// <inheritdoc />
        public int CompareTo(DefectEnergyData other)
        {
            if (other == null) return -1;
            if (ReferenceEquals(this, other)) return 0;
            var positionComp = string.CompareOrdinal(CellReferencePosition?.Key, other.CellReferencePosition?.Key);
            return positionComp == 0 ? string.CompareOrdinal(Particle?.Key, other.Particle?.Key) : positionComp;
        }

        /// <summary>
        ///     Get an <see cref="DefectEnergy" /> object for the model input pipeline
        /// </summary>
        /// <returns></returns>
        public DefectEnergy GetInputObject() =>
            new DefectEnergy
            {
                Energy = Energy,
                Particle = (IParticle) Particle.GetInputObject(),
                CellSite = (ICellSite) CellReferencePosition.GetInputObject()
            };
    }
}