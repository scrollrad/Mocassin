﻿using System;
using System.Collections.Generic;
using Mocassin.Mathematics.ValueTypes;
using Mocassin.Model.Transitions;

namespace Mocassin.Model.Translator.ModelContext
{
    /// <inheritdoc />
    public class MetropolisMappingModel : IMetropolisMappingModel
    {
        /// <inheritdoc />
        public bool IsSourceInversion { get; set; }

        /// <inheritdoc />
        public MetropolisMapping Mapping { get; set; }

        /// <inheritdoc />
        public IMetropolisMappingModel InverseMapping { get; set; }

        /// <inheritdoc />
        public Fractional3D StartVector3D { get; set; }

        /// <inheritdoc />
        public Fractional3D EndVector3D { get; set; }

        /// <inheritdoc />
        public int PathLength => 2;

        /// <inheritdoc />
        public ITransitionMappingModel InverseMappingBase => InverseMapping;

        /// <inheritdoc />
        /// <remarks> Coordinates (0,0,0,P) are always in the original unit cell </remarks>
        public Vector4I StartVector4D { get; set; }

        /// <inheritdoc />
        public Vector4I EndVector4D { get; set; }

        /// <inheritdoc />
        public IMetropolisTransitionModel TransitionModel { get; set; }

        /// <inheritdoc />
        public bool InverseIsSet => InverseMapping != null;

        /// <inheritdoc />
        public IEnumerable<Fractional3D> GetMovementSequence()
        {
            yield break;
        }

        /// <inheritdoc />
        public IEnumerable<Vector4I> GetTransitionSequence()
        {
            yield return new Vector4I(0, 0, 0, Mapping.PositionIndex1);
        }

        /// <inheritdoc />
        public ITransitionModel GetTransitionModel() => TransitionModel;

        /// <inheritdoc />
        public IMetropolisMappingModel CreateGeometricInversion() =>
            new MetropolisMappingModel
            {
                TransitionModel = TransitionModel.InverseTransitionModel
                                  ?? throw new InvalidOperationException("Inverse transition model is unknown!"),
                IsSourceInversion = true,
                Mapping = Mapping.CreateGeometricInversion(),
                InverseMapping = this,
                StartVector3D = EndVector3D,
                EndVector3D = StartVector3D,
                StartVector4D = EndVector4D,
                EndVector4D = StartVector4D
            };

        /// <inheritdoc />
        public bool LinkIfGeometricInversion(IMetropolisMappingModel mappingModel)
        {
            if (Mapping.Transition != mappingModel.Mapping.Transition || !StartVector4D.Equals(mappingModel.EndVector4D))
                return false;

            InverseMapping = mappingModel;
            mappingModel.InverseMapping = this;
            return true;
        }
    }
}