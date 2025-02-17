﻿using Mocassin.UI.GUI.Base.DataContext;
using Mocassin.UI.GUI.Controls.Base.ViewModels;
using Mocassin.UI.GUI.Controls.ProjectWorkControl.ModelControls.StructureModel.DataControl;
using Mocassin.UI.Data.Main;
using Mocassin.UI.Data.ProjectLibrary;

namespace Mocassin.UI.GUI.Controls.ProjectWorkControl.ModelControls.StructureModel
{
    /// <summary>
    ///     The <see cref="ProjectGraphControlViewModel" /> that controls <see cref="StructureModelControlView" />
    /// </summary>
    public class StructureModelControlViewModel : ProjectGraphControlViewModel
    {
        /// <summary>
        ///     Get the <see cref="CellPositionControlViewModel" /> that controls cell positions
        /// </summary>
        public CellPositionControlViewModel PositionControlViewModel { get; }

        /// <summary>
        ///     Get the <see cref="StructureParameterControlViewModel" /> that controls geometry and misc info
        /// </summary>
        public StructureParameterControlViewModel ParameterControlViewModel { get; }

        /// <inheritdoc />
        public StructureModelControlViewModel(IProjectAppControl projectControl)
            : base(projectControl)
        {
            ParameterControlViewModel = new StructureParameterControlViewModel(projectControl);
            PositionControlViewModel = new CellPositionControlViewModel(ParameterControlViewModel);
        }

        /// <inheritdoc />
        public override void ChangeContentSource(MocassinProject contentSource)
        {
            ParameterControlViewModel.ChangeContentSource(contentSource);
            PositionControlViewModel.ChangeContentSource(contentSource);
        }

        /// <inheritdoc />
        protected override void OnProjectLibraryChangedInternal(IMocassinProjectLibrary newProjectLibrary)
        {
            ChangeContentSource(null);
        }
    }
}