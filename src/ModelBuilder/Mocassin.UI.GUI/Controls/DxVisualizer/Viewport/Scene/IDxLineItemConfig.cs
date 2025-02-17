﻿using System.Windows.Media;
using HelixToolkit.Wpf.SharpDX.Model;

namespace Mocassin.UI.GUI.Controls.DxVisualizer.Viewport.Scene
{
    /// <summary>
    ///     Represents a view model for manipulation and configuration of display/generation of DX scene line items
    /// </summary>
    public interface IDxLineItemConfig : IDxSceneItemConfig
    {
        /// <summary>
        ///     Get or set the <see cref="LineMaterialCore" /> for the geometry
        /// </summary>
        LineMaterialCore Material { get; set; }

        /// <summary>
        ///     Get or set the line color as a <see cref="System.Windows.Media.Color" />
        /// </summary>
        Color Color { get; set; }

        /// <summary>
        ///     Get or set the thickness of the line geometry
        /// </summary>
        double LineThickness { get; set; }

        /// <summary>
        ///     Creates a new <see cref="LineMaterialCore" /> using the current settings
        /// </summary>
        /// <returns></returns>
        LineMaterialCore CreateMaterial();
    }
}