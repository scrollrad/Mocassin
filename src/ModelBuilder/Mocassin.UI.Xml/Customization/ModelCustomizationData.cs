﻿using System.Xml.Serialization;
using Mocassin.Model.ModelProject;
using Mocassin.UI.Data.Base;
using Mocassin.UI.Data.Main;

namespace Mocassin.UI.Data.Customization
{
    /// <summary>
    ///     Abstract base class for all serializable data objects that enable customization of <see cref="IModelProject" />
    ///     auto generated content
    /// </summary>
    [XmlRoot]
    public abstract class ModelCustomizationData : ProjectChildEntity<MocassinProject>
    {
        /// <summary>
        ///     Pushes all set data on the customization to the passed <see cref="IModelProject" />
        /// </summary>
        /// <param name="modelProject"></param>
        public abstract void PushToModel(IModelProject modelProject);
    }
}