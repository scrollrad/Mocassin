﻿using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Serialization;
using Mocassin.Model.Basic;
using Mocassin.Model.Transitions;
using Mocassin.UI.Data.Base;

namespace Mocassin.UI.Data.TransitionModel
{
    /// <summary>
    ///     Serializable data object for <see cref="Mocassin.Model.Transitions.IKineticTransition" /> model object creation
    /// </summary>
    [XmlRoot]
    public class KineticTransitionData : ModelDataObject
    {
        private ModelObjectReference<AbstractTransition> abstractTransition = new ModelObjectReference<AbstractTransition>();
        private ObservableCollection<VectorData3D> pathVectors = new ObservableCollection<VectorData3D>();

        /// <summary>
        ///     Get or set the abstract transition key for the transition logic
        /// </summary>
        [XmlElement]
        public ModelObjectReference<AbstractTransition> AbstractTransition
        {
            get => abstractTransition;
            set => SetProperty(ref abstractTransition, value);
        }

        /// <summary>
        ///     Get or set the geometry sequence of the kinetic transition
        /// </summary>
        [XmlArray]
        public ObservableCollection<VectorData3D> PathVectors
        {
            get => pathVectors;
            set => SetProperty(ref pathVectors, value);
        }

        /// <inheritdoc />
        protected override ModelObject GetModelObjectInternal()
        {
            var obj = new KineticTransition
            {
                AbstractTransition = new AbstractTransition {Key = AbstractTransition.Key},
                PathGeometry = PathVectors.Select(x => x.AsFractional3D()).ToList()
            };
            return obj;
        }
    }
}