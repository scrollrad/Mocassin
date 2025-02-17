﻿using System;
using System.Deployment.Application;
using System.Windows;
using Mocassin.UI.GUI.Base.DataContext;
using Mocassin.UI.GUI.Base.Loading;
using Mocassin.UI.GUI.Controls.Base.Commands;
using Application = System.Windows.Forms.Application;

namespace Mocassin.UI.GUI.Controls.ProjectMenuBar.SubControls.HelpMenu.Commands
{
    /// <summary>
    ///     The <see cref="ProjectControlCommand" /> to manually check and install an application update
    /// </summary>
    public class CheckApplicationUpdateCommand : ProjectControlCommand
    {
        /// <inheritdoc />
        public CheckApplicationUpdateCommand(IProjectAppControl projectControl)
            : base(projectControl)
        {
        }

        /// <inheritdoc />
        public override bool CanExecuteInternal() => ApplicationDeployment.IsNetworkDeployed && base.CanExecuteInternal();

        /// <inheritdoc />
        public override void Execute()
        {
            ProjectControl.StopServices();
            if (TryGetUpdateInformation(out var updateCheckInfo) && GetUserConfirmationIfUpdateAvailable(updateCheckInfo))
            {
                UpdateApplication();
                ProjectControl.StartServices();
            }

            ProjectControl.StartServices();
        }

        /// <summary>
        ///     Tries to get the <see cref="UpdateCheckInfo" /> or the application
        /// </summary>
        /// <returns></returns>
        private bool TryGetUpdateInformation(out UpdateCheckInfo info)
        {
            info = default;
            if (!ApplicationDeployment.IsNetworkDeployed) return false;
            try
            {
                info = ApplicationDeployment.CurrentDeployment.CheckForDetailedUpdate();
            }
            catch (DeploymentDownloadException exception)
            {
                Console.WriteLine(exception);
                MessageBox.Show($"Failed to download the new application version:\n{exception.Message}", "Update Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
            catch (InvalidDeploymentException exception)
            {
                Console.WriteLine(exception);
                MessageBox.Show($"Deployment of application is corrupt:\n{exception.Message}", "Update Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
            catch (InvalidOperationException exception)
            {
                Console.WriteLine(exception);
                MessageBox.Show($"Application update is impossible, not a deploy version:\n{exception.Message}", "Update Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Request an update confirmation from the user and returns true if the application should be updated
        /// </summary>
        /// <param name="updateCheckInfo"></param>
        /// <returns></returns>
        private static bool GetUserConfirmationIfUpdateAvailable(UpdateCheckInfo updateCheckInfo)
        {
            if (updateCheckInfo == null) return false;
            if (!updateCheckInfo.UpdateAvailable)
            {
                MessageBox.Show($"Latest version [{ApplicationDeployment.CurrentDeployment.CurrentVersion}] is already installed.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            var message = $"An application update to version [{updateCheckInfo.AvailableVersion}] is available." +
                          $" Would you like to download and install the update [{updateCheckInfo.UpdateSizeBytes / 1024} KB]?";
            var choice = MessageBox.Show(message, "Update confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            return choice == MessageBoxResult.Yes;
        }

        /// <summary>
        ///     Updates the application to the newest version and restarts
        /// </summary>
        private void UpdateApplication()
        {
            try
            {
                DoUpdate();
                MessageBox.Show("Application has been updated and will now restart.", "Update Success", MessageBoxButton.OK, MessageBoxImage.Information);
                Application.Restart();
                System.Windows.Application.Current.Shutdown();
            }
            catch (DeploymentDownloadException exception)
            {
                Console.WriteLine(exception);
                MessageBox.Show($"Failed to download the update data:\n{exception.Message}");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                MessageBox.Show($"Fatal error during application update:\n{exception.Message}");
            }
        }

        /// <summary>
        ///     Does the application update while showing a progress window
        /// </summary>
        private void DoUpdate()
        {
            var loadingWindow = new LoadingWindow();
            loadingWindow.Show();
            try
            {
                ApplicationDeployment.CurrentDeployment.Update();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                MessageBox.Show($"Fatal error during update pull:\n{exception.Message}");
            }
            finally
            {
                loadingWindow.Close();
            }
        }
    }
}