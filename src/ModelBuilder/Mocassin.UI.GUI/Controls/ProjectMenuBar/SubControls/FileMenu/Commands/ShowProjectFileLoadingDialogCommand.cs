﻿using Mocassin.UI.GUI.Base.DataContext;
using Mocassin.UI.GUI.Base.Loading;
using Mocassin.UI.GUI.Controls.Base.Commands;
using Mocassin.UI.GUI.Controls.Base.IO;

namespace Mocassin.UI.GUI.Controls.ProjectMenuBar.SubControls.FileMenu.Commands
{
    /// <summary>
    ///     The <see cref="ProjectControlCommand" /> to show a project loading dialog
    /// </summary>
    public class ShowProjectFileLoadingDialogCommand : ProjectControlCommand
    {
        /// <summary>
        ///     The <see cref="UserFileSelectionSource" /> that is used by the command
        /// </summary>
        private readonly UserFileSelectionSource userFileSelectionSource;

        /// <inheritdoc />
        public ShowProjectFileLoadingDialogCommand(IProjectAppControl projectControl)
            : base(projectControl)
        {
            userFileSelectionSource = UserFileSelectionSource.CreateForProjectFiles(false);
        }

        /// <inheritdoc />
        public override void Execute()
        {
            if (userFileSelectionSource.TryRequestFileSelection(out var selected, true))
            {
                WindowedBackgroundTask.RunWithLoadingWindow(()
                    => ProjectControl.ProjectManagerViewModel.OpenProjectLibraryCommand.Execute(selected));
            }
        }

        /// <inheritdoc />
        public override bool CanExecuteInternal() => ProjectControl.ProjectManagerViewModel != null;
    }
}