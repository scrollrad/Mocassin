﻿using System.Collections.Generic;
using System.Xml.Serialization;
using Mocassin.Model.ModelProject;
using Mocassin.Model.Particles;
using Mocassin.Model.Structures;
using Mocassin.Model.Translator.Optimization;
using Mocassin.UI.Data.Base;

namespace Mocassin.UI.Data.Jobs
{
    /// <summary>
    ///     Serializable data object that enables manual definition of <see cref="JumpSelectionOptimizer" /> objects to speed
    ///     up simulation
    /// </summary>
    [XmlRoot]
    public class SelectionOptimizerData : ManualOptimizerData, IDuplicable<SelectionOptimizerData>
    {
        private ModelObjectReference<Particle> removedParticle;
        private ModelObjectReference<CellSite> startReferencePosition;

        /// <summary>
        ///     Get or set the <see cref="ModelObjectReference{T}" /> to the <see cref="CellSite" /> that the
        ///     optimizer targets
        /// </summary>
        [XmlElement]
        public ModelObjectReference<CellSite> StartReferencePosition
        {
            get => startReferencePosition;
            set => SetProperty(ref startReferencePosition, value);
        }

        /// <summary>
        ///     Get or set the <see cref="ModelObjectReference{T}" /> to the <see cref="Particle" /> that the optimizers
        ///     removes
        /// </summary>
        [XmlElement]
        public ModelObjectReference<Particle> RemovedParticle
        {
            get => removedParticle;
            set => SetProperty(ref removedParticle, value);
        }

        /// <inheritdoc />
        public SelectionOptimizerData Duplicate() =>
            new SelectionOptimizerData
            {
                Name = Name,
                StartReferencePosition = StartReferencePosition.Duplicate(),
                RemovedParticle = RemovedParticle.Duplicate()
            };

        /// <inheritdoc />
        object IDuplicable.Duplicate() => Duplicate();

        /// <inheritdoc />
        public override IPostBuildOptimizer ToInternal(IModelProject modelProject)
        {
            var particle = modelProject.DataTracker.FindObject<IParticle>(RemovedParticle.Key);
            var cellReferencePosition = modelProject.DataTracker.FindObject<ICellSite>(StartReferencePosition.Key);
            return new JumpSelectionOptimizer
            {
                RemoveCombinations = new List<(IParticle, ICellSite)> {(particle, cellReferencePosition)}
            };
        }
    }
}