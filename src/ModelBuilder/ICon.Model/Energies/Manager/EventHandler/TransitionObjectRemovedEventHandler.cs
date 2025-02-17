﻿using Mocassin.Framework.Operations;
using Mocassin.Model.Basic;
using Mocassin.Model.ModelProject;
using Mocassin.Model.Transitions;

namespace Mocassin.Model.Energies.Handler
{
    /// <summary>
    ///     Event handler that manages the processing of object removal events that the energy manager receives from the
    ///     transition manager event port
    /// </summary>
    internal class TransitionObjectRemovedEventHandler : ObjectRemovedEventHandler<ITransitionEventPort, EnergyModelData, EnergyEventManager>
    {
        /// <inheritdoc />
        public TransitionObjectRemovedEventHandler(IModelProject modelProject, DataAccessorSource<EnergyModelData> dataAccessorSource,
            EnergyEventManager eventManager)
            : base(modelProject, dataAccessorSource, eventManager)
        {
        }

        /// <summary>
        ///     Event reaction to a change in one of the abstract transition objects
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        [EventHandlingMethod]
        protected IConflictReport HandleAbstractTransition(IModelObjectEventArgs<IAbstractTransition> eventArgs) => DummyHandleEvent(eventArgs);

        /// <summary>
        ///     Event reaction to a change in one of the state exchange pair objects
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        [EventHandlingMethod]
        protected IConflictReport HandleStateExchangePair(IModelObjectEventArgs<IStateExchangePair> eventArgs) => DummyHandleEvent(eventArgs);

        /// <summary>
        ///     Event reaction to a change in one of the kinetic transition objects
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        [EventHandlingMethod]
        protected IConflictReport HandleKineticTransition(IModelObjectEventArgs<IKineticTransition> eventArgs) => DummyHandleEvent(eventArgs);

        /// <summary>
        ///     Event reaction to a change in one of the metropolis transition objects
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        [EventHandlingMethod]
        protected IConflictReport HandleMetropolisStatePair(IModelObjectEventArgs<IMetropolisTransition> eventArgs) => DummyHandleEvent(eventArgs);

        /// <summary>
        ///     Event reaction to a removal of one of the state exchange groups
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        [EventHandlingMethod]
        protected IConflictReport HandleStateExchangeGroup(IModelObjectEventArgs<IStateExchangeGroup> eventArgs) => DummyHandleEvent(eventArgs);
    }
}