﻿using System.Windows.Media;
using HelixToolkit.Wpf.SharpDX.Model;

namespace Mocassin.UI.GUI.Controls.DxVisualizer.Viewport.Scene
{
    /// <summary>
    ///     Represents a view model for manipulation and configuration of display/generation of DX scene mesh items
    /// </summary>
    public interface IDxMeshItemConfig : IDxSceneItemConfig
    {
        /// <summary>
        ///     Get or set a boolean flag that indicates if mesh resizing can be done by: origin-shift -> rescale -> back-shift
        /// </summary>
        bool CanResizeMeshAtOrigin { get; set; }

        /// <summary>
        ///     Get or set a boolean flag if the wireframe of the mesh should be rendered
        /// </summary>
        bool IsWireframeVisible { get; set; }

        /// <summary>
        ///     Get or set the <see cref="MaterialCore" /> of the mesh
        /// </summary>
        MaterialCore Material { get; set; }

        /// <summary>
        ///     Get or set the diffuse color as a <see cref="System.Windows.Media.Color" />
        /// </summary>
        Color DiffuseColor { get; set; }

        /// <summary>
        ///     Get or set the wireframe color as a <see cref="System.Windows.Media.Color" />
        /// </summary>
        Color WireframeColor { get; set; }

        /// <summary>
        ///     Get or set a factor for the mesh quality (1.0 is the default quality)
        /// </summary>
        double MeshQuality { get; set; }

        /// <summary>
        ///     Get or set a factor for the uniform mesh scaling (1.0 is the default quality)
        /// </summary>
        double MeshScaling { get; set; }

        /// <summary>
        ///     Creates a new <see cref="MaterialCore" /> of using the current material and color
        /// </summary>
        /// <returns></returns>
        MaterialCore CreateMaterial();
    }
}