﻿using System;
using System.Xml.Serialization;
using Mocassin.Model.Basic;

namespace Mocassin.UI.Data.Base
{
    /// <summary>
    ///     Base class for all serializable data objects that supply <see cref="ModelObject" /> conversion for data input
    /// </summary>
    [XmlRoot]
    public abstract class ModelDataObject : ExtensibleProjectDataObject
    {
        private string key;

        /// <summary>
        ///     The key of the model object. Has to be unique within the object graph type group
        /// </summary>
        [XmlAttribute]
        public string Key
        {
            get => key;
            set => SetProperty(ref key, value);
        }

        /// <summary>
        ///     Creates a new <see cref="ModelDataObject" /> with a unique object key
        /// </summary>
        protected ModelDataObject()
        {
            Key = Guid.NewGuid().ToString();
            Resources = new ResourcesData();
        }

        /// <summary>
        ///     Get the input <see cref="ModelObject" /> for the automated data input system of the model management
        /// </summary>
        /// <returns></returns>
        public ModelObject GetInputObject()
        {
            var obj = GetModelObjectInternal();
            obj.Key = Key ?? Guid.NewGuid().ToString();
            obj.Name = Name;
            obj.Index = -1;
            return obj;
        }

        /// <summary>
        ///     Get a prepared <see cref="ModelObject" /> with all specific input data set
        /// </summary>
        /// <returns></returns>
        protected abstract ModelObject GetModelObjectInternal();

        /// <inheritdoc />
        public override string ToString() =>
            string.IsNullOrWhiteSpace(Name)
                ? $"[{Key}]"
                : Name;
    }
}