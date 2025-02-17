﻿using System.Collections.Generic;
using System.Linq;
using Mocassin.Model.Particles;
using Mocassin.Model.Structures;
using Mocassin.Model.Transitions;

namespace Mocassin.Model.Translator.ModelContext
{
    /// <inheritdoc cref="IKineticTransitionModel" />
    public class KineticTransitionModel : TransitionModel, IKineticTransitionModel
    {
        /// <inheritdoc />
        public IKineticTransition Transition { get; set; }

        /// <inheritdoc />
        public IKineticTransitionModel InverseTransitionModel { get; set; }

        /// <inheritdoc />
        public IList<IKineticMappingModel> MappingModels { get; set; }

        /// <inheritdoc />
        public IList<IKineticRuleModel> RuleModels { get; set; }

        /// <inheritdoc />
        public IParticle EffectiveParticle { get; set; }

        /// <inheritdoc />
        public IList<int> AbstractMovement { get; set; }

        /// <inheritdoc />
        public override IEnumerable<ITransitionMappingModel> GetMappingModels() => MappingModels.AsEnumerable();

        /// <inheritdoc />
        public override IEnumerable<ITransitionRuleModel> GetRuleModels() => RuleModels.AsEnumerable();

        /// <inheritdoc />
        public IKineticTransitionModel CreateGeometricInverse() =>
            new KineticTransitionModel
            {
                Transition = Transition,
                InverseTransitionModel = this,
                AbstractMovement = GetInverseAbstractMovement(),
                SelectableParticles = SelectableParticles,
                EffectiveParticle = EffectiveParticle
            };

        /// <inheritdoc />
        public bool MappingsContainInversion()
        {
            if (MappingModels.Count == 0) return false;

            var remaining = MappingModels.Count;
            for (var i = 0; i < MappingModels.Count; i++)
            {
                var relPath = MappingModels[i].PositionSequence4D.Reverse().ToList();
                for (var j = i + 1; j < MappingModels.Count; j++)
                {
                    var otherPath = MappingModels[j].PositionSequence4D;
                    if (relPath.Zip(otherPath, (first, second) => second.P - first.P).Any(value => value != 0))
                        continue;
                    remaining -= 2;
                    break;
                }
            }

            return remaining == 0;
        }

        /// <inheritdoc />
        public ICellSite GetStartCellReferencePosition() => MappingModels.First().Mapping.StartCellSite;

        /// <summary>
        ///     Gets the inverted abstract movement description
        /// </summary>
        /// <returns></returns>
        protected IList<int> GetInverseAbstractMovement()
        {
            return AbstractMovement.Select(a => -a).Reverse().ToList();
        }
    }
}