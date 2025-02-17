﻿using System;
using System.Windows.Controls;
using Mocassin.UI.GUI.Base.DataContext;
using Mocassin.UI.GUI.Base.ViewModels;
using Mocassin.UI.GUI.Base.Views;
using Mocassin.UI.GUI.Controls.Base.Interfaces;
using Mocassin.UI.GUI.Controls.Base.ViewModels;
using Mocassin.UI.Data.Main;
using Mocassin.UI.Data.ProjectLibrary;

namespace Mocassin.UI.GUI.Controls.ProjectWorkControl.ModelControls.Base.Content
{
    /// <summary>
    ///     Basic <see cref="ViewModelBase" /> for <see cref="BasicModelContentControlView" /> that provides a default control
    ///     content for model data manipulation
    /// </summary>
    public class BasicModelContentControlViewModel : PrimaryControlViewModel
    {
        private ContentControl dataContentControl;
        private MocassinProject selectedProject;

        /// <summary>
        ///     Stores a reference to the content control data context <see cref="IDisposable" /> interface (If it exists)
        /// </summary>
        private IDisposable DataContextDisposable { get; set; }

        /// <summary>
        ///     Get or set the selected <see cref="MocassinProject" />
        /// </summary>
        public MocassinProject SelectedProject
        {
            get => selectedProject;
            set
            {
                SetProperty(ref selectedProject, value);
                OnProjectGraphSelectionChanged();
            }
        }

        /// <summary>
        ///     Get or set the <see cref="ContentControl" /> for data manipulation
        /// </summary>
        public ContentControl DataContentControl
        {
            get => dataContentControl ?? new NoContentView();
            set
            {
                SetProperty(ref dataContentControl, value);
                DataContextDisposable?.Dispose();
                DataContextDisposable = value?.DataContext as IDisposable;
            }
        }

        /// <inheritdoc />
        public BasicModelContentControlViewModel(IProjectAppControl projectControl)
            : base(projectControl)
        {
        }

        /// <summary>
        ///     Action that is invoked if the selected <see cref="MocassinProject" /> changes
        /// </summary>
        protected void OnProjectGraphSelectionChanged()
        {
            NotifyGraphSelectionChanged(DataContentControl);
        }

        /// <summary>
        ///     Action that is invoked if a selected object of a generic type has changed
        /// </summary>
        /// <param name="value"></param>
        protected void OnSelectionChanged<T>(T value)
        {
            NotifySelectionChanged(DataContentControl, value);
        }

        /// <summary>
        ///     Tries to notify a <see cref="ContentControl" /> about a change in the selected <see cref="MocassinProject" />
        /// </summary>
        /// <param name="contentControl"></param>
        protected void NotifyGraphSelectionChanged(ContentControl contentControl)
        {
            (contentControl.DataContext as IContentSupplier<MocassinProject>)?.ChangeContentSource(SelectedProject);
        }

        /// <summary>
        ///     Notifies the passed <see cref="ContentControl" /> about a selection change if it implements
        ///     the generic <see cref="IContentSupplier{T}" /> interface
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="contentControl"></param>
        /// <param name="value"></param>
        protected void NotifySelectionChanged<T>(ContentControl contentControl, T value)
        {
            (contentControl.DataContext as IContentSupplier<T>)?.ChangeContentSource(value);
        }

        /// <inheritdoc />
        protected override void OnProjectLibraryChangedInternal(IMocassinProjectLibrary newProjectLibrary)
        {
            ExecuteOnAppThread(() => SelectedProject = null);
            base.OnProjectLibraryChangedInternal(newProjectLibrary);
        }

        /// <inheritdoc />
        protected override void OnProjectContentChangedInternal()
        {
            if (!ProjectControl.ProjectGraphs.Contains(SelectedProject)) ExecuteOnAppThread(() => SelectedProject = null);
            base.OnProjectContentChangedInternal();
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            DataContextDisposable?.Dispose();
            (DataContentControl as IDisposable)?.Dispose();
            base.Dispose();
        }
    }
}