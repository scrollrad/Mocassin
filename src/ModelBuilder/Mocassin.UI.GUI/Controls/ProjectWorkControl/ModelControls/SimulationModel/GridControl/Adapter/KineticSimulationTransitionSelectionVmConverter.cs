﻿using Mocassin.UI.GUI.Controls.Base.Interfaces;
using Mocassin.UI.GUI.Controls.ProjectWorkControl.ModelControls.Base.GridControl;
using Mocassin.UI.Data.Main;
using Mocassin.UI.Data.SimulationModel;
using Mocassin.UI.Data.TransitionModel;

namespace Mocassin.UI.GUI.Controls.ProjectWorkControl.ModelControls.SimulationModel.GridControl.Adapter
{
    /// <summary>
    ///     The <see cref="HostGraphGuestSelectionVmConverter{THost}" /> implementation for wrapping
    ///     <see cref="KineticSimulationData" /> instances into host view models for <see cref="KineticTransitionData" />
    ///     references
    /// </summary>
    public class KineticSimulationTransitionSelectionVmConverter : HostGraphGuestSelectionVmConverter<KineticSimulationData>
    {
        /// <inheritdoc />
        protected override IContentSupplier<MocassinProject> CreateSelectionViewModel(KineticSimulationData host) =>
            new KineticSimulationTransitionSelectionViewModel(host);
    }
}