﻿using System;
using Mocassin.Model.Basic;
using Mocassin.Model.ModelProject;

namespace Mocassin.Model.Energies
{
    /// <summary>
    ///     Basic implementation of the energy manager that manages and supplies objects and parameters related to the
    ///     energetic modeling process
    /// </summary>
    internal class EnergyManager : ModelManager<EnergyModelData, EnergyModelCache, EnergyDataManager, EnergyCacheManager, EnergyInputManager
        , EnergyQueryManager, EnergyEventManager, EnergyUpdateManager>, IEnergyManager
    {
        /// <inheritdoc />
        public new IEnergyInputPort InputAccess => InputManager;

        /// <inheritdoc />
        public IEnergyQueryPort DataAccess => QueryManager;

        /// <inheritdoc />
        public new IEnergyEventPort EventAccess => EventManager;

        /// <inheritdoc />
        public EnergyManager(IModelProject modelProject, EnergyModelData data)
            : base(modelProject, data)
        {
        }

        /// <inheritdoc />
        public override Type GetManagerInterfaceType() => typeof(IEnergyManager);

        /// <inheritdoc />
        public override IValidationService CreateValidationService(ProjectSettings settings)
        {
            if (!settings.TryGetModuleSettings(out MocassinEnergySettings moduleSettings))
                throw new InvalidOperationException("Settings object for the energy module is missing");

            return new EnergyValidationService(ModelProject, moduleSettings);
        }
    }
}