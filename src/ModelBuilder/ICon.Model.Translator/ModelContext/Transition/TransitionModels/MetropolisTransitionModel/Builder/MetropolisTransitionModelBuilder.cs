﻿using System;
using System.Collections.Generic;
using System.Linq;
using Mocassin.Mathematics.ValueTypes;
using Mocassin.Model.ModelProject;
using Mocassin.Model.Structures;
using Mocassin.Model.Transitions;

namespace Mocassin.Model.Translator.ModelContext
{
    /// <inheritdoc cref="IMetropolisTransitionModelBuilder" />
    public class MetropolisTransitionModelBuilder : TransitionModelBuilder, IMetropolisTransitionModelBuilder
    {
        /// <inheritdoc />
        public MetropolisTransitionModelBuilder(IModelProject modelProject)
            : base(modelProject)
        {
        }

        /// <inheritdoc />
        public IList<IMetropolisTransitionModel> BuildModels(IEnumerable<IMetropolisTransition> transitions)
        {
            var resultModels = new List<IMetropolisTransitionModel>();
            var inverseModels = new List<IMetropolisTransitionModel>();

            var index = 0;
            foreach (var transition in transitions)
            {
                var model = CreateTransitionModel(transition);
                model.ModelId = index++;
                resultModels.Add(model);
            }

            foreach (var transitionModel in resultModels)
            {
                var inverseModel = CreateGeometricModelInversion(transitionModel);
                if (transitionModel == inverseModel) continue;

                inverseModel.ModelId = index++;
                inverseModels.Add(transitionModel.InverseTransitionModel);
            }

            resultModels.AddRange(inverseModels);
            resultModels.ForEach(AddBasicMobilityInformation);
            return resultModels;
        }

        /// <summary>
        ///     Creates a single metropolis transition model with rule models and mapping models
        /// </summary>
        /// <param name="transition"></param>
        /// <returns></returns>
        protected IMetropolisTransitionModel CreateTransitionModel(IMetropolisTransition transition)
        {
            var transitionModel = new MetropolisTransitionModel
            {
                Transition = transition
            };

            CreateAndAddMappingModels(transitionModel);
            CreateAndAddRuleModels(transitionModel);

            return transitionModel;
        }


        /// <summary>
        ///     Creates an inverse metropolis transition model if required or if the mappings already contain
        ///     the inversion the original is returned
        /// </summary>
        /// <param name="transitionModel"></param>
        /// <returns></returns>
        protected IMetropolisTransitionModel CreateGeometricModelInversion(IMetropolisTransitionModel transitionModel)
        {
            if (transitionModel.Transition.MappingsContainInversion())
            {
                transitionModel.InverseTransitionModel = transitionModel;
                return transitionModel;
            }

            var inverseModel = transitionModel.CreateGeometricInversion();
            transitionModel.InverseTransitionModel = inverseModel;

            CreateAndAddMappingModelInversions(transitionModel, inverseModel);
            AddGeometricRuleModelInversions(transitionModel, inverseModel);

            return inverseModel;
        }

        /// <summary>
        ///     Creates all mapping models for a metropolis transition model and links the mappings together if they contain
        ///     their own inversions
        /// </summary>
        /// <param name="transitionModel"></param>
        protected void CreateAndAddMappingModels(IMetropolisTransitionModel transitionModel)
        {
            var transitionManager = ModelProject.Manager<ITransitionManager>();
            var mappings = transitionManager.DataAccess
                                            .Query(port => port.GetMetropolisMappingList(transitionModel.Transition.Index));

            transitionModel.MappingModels = mappings
                                            .Select(mapping => CreateMappingModel(mapping, transitionModel))
                                            .ToList();

            if (!transitionModel.Transition.MappingsContainInversion())
                return;

            LinkSelfConsistentMappingModels(transitionModel.MappingModels);
        }

        /// <summary>
        ///     Creates a single mapping model for a metropolis transition mapping object and links it to the passed transition
        ///     model
        /// </summary>
        /// <param name="mapping"></param>
        /// <param name="transitionModel"></param>
        /// <returns></returns>
        protected IMetropolisMappingModel CreateMappingModel(MetropolisMapping mapping, IMetropolisTransitionModel transitionModel)
        {
            var vectorEncoder = ModelProject.Manager<IStructureManager>().DataAccess.Query(port => port.GetVectorEncoder());
            var mappingModel = new MetropolisMappingModel
            {
                Mapping = mapping,
                TransitionModel = transitionModel,
                StartVector4D = new Vector4I(0, 0, 0, mapping.PositionIndex0),
                EndVector4D = new Vector4I(0, 0, 0, mapping.PositionIndex1)
            };

            if (!vectorEncoder.TryDecode(mappingModel.StartVector4D, out Fractional3D startVector3D))
                throw new InvalidOperationException("Data inconsistency. 4D to 3D vector conversion failed!");

            if (!vectorEncoder.TryDecode(mappingModel.EndVector4D, out Fractional3D endVector3D))
                throw new InvalidOperationException("Data inconsistency. 4D to 3D vector conversion failed!");

            mappingModel.StartVector3D = startVector3D;
            mappingModel.EndVector3D = endVector3D;
            return mappingModel;
        }

        /// <summary>
        ///     Creates inverted mappings models for the original transition models, links them to the originals
        ///     and adds the inversions to the list of the inverse model
        /// </summary>
        /// <param name="originalModel"></param>
        /// <param name="inverseModel"></param>
        protected void CreateAndAddMappingModelInversions(IMetropolisTransitionModel originalModel, IMetropolisTransitionModel inverseModel)
        {
            var inverseMappings = originalModel.MappingModels.Select(CreateAndLinkGeometricInversion).ToList();
            inverseModel.MappingModels = inverseMappings;
        }

        /// <summary>
        ///     Creates a single inverted mapping model and sets both inverted mappings to the appropriate value
        /// </summary>
        /// <param name="mappingModel"></param>
        /// <returns></returns>
        protected IMetropolisMappingModel CreateAndLinkGeometricInversion(IMetropolisMappingModel mappingModel)
        {
            var inverseModel = mappingModel.CreateGeometricInversion();
            mappingModel.InverseMapping = inverseModel;
            return inverseModel;
        }

        /// <summary>
        ///     Links a set of self consistent mapping models (Mappings that contain their own inversions)
        /// </summary>
        /// <param name="mappingModels"></param>
        protected void LinkSelfConsistentMappingModels(IList<IMetropolisMappingModel> mappingModels)
        {
            for (var i = 0; i < mappingModels.Count; i++)
            {
                if (mappingModels[i].InverseIsSet) continue;

                for (var j = i; j < mappingModels.Count; j++)
                {
                    if (mappingModels[j].InverseIsSet) continue;
                    if (mappingModels[i].LinkIfGeometricInversion(mappingModels[j])) break;
                }
            }

            if (mappingModels.Any(a => !a.InverseIsSet))
                throw new InvalidOperationException("Passed mapping set is not self consistent");
        }

        /// <summary>
        ///     Creates and adds the rule models of both parent and dependent rules on the passed transition model
        /// </summary>
        /// <param name="transitionModel"></param>
        protected void CreateAndAddRuleModels(IMetropolisTransitionModel transitionModel)
        {
            var ruleModels = transitionModel.Transition.GetExtendedTransitionRules()
                                            .Select(CreateRuleModel)
                                            .ToList();

            CreateCodesAndLinkLogicRuleInversions(ruleModels);

            transitionModel.RuleModels = ruleModels;
        }

        /// <summary>
        ///     Creates a rule model for the passed metropolis rule
        /// </summary>
        /// <param name="metropolisRule"></param>
        /// <returns></returns>
        protected IMetropolisRuleModel CreateRuleModel(IMetropolisRule metropolisRule)
        {
            var ruleModel = CreateRuleModelBasis<MetropolisRuleModel>(metropolisRule);
            ruleModel.MetropolisRule = metropolisRule;
            return ruleModel;
        }

        /// <summary>
        ///     Creates the set of rule inversions and sets the on the passed inverse metropolis transition model
        /// </summary>
        /// <param name="originalModel"></param>
        /// <param name="inverseModel"></param>
        protected void AddGeometricRuleModelInversions(IMetropolisTransitionModel originalModel, IMetropolisTransitionModel inverseModel)
        {
            var inverseModels = originalModel.RuleModels.Select(CreateGeometricInversion).ToList();
            CreateCodesAndLinkLogicRuleInversions(inverseModels);
            inverseModel.RuleModels = inverseModels;
        }

        /// <summary>
        ///     Creates a single inverted metropolis rule model and sets both inverted mappings to the appropriate value
        /// </summary>
        /// <param name="originalModel"></param>
        /// <returns></returns>
        protected IMetropolisRuleModel CreateGeometricInversion(IMetropolisRuleModel originalModel)
        {
            var inverseModel = originalModel.CreateGeometricInversion();
            return inverseModel;
        }
    }
}