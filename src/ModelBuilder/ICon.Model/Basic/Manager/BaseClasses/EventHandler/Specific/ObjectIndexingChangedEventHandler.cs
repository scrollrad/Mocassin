﻿using System;
using Mocassin.Model.ModelProject;

namespace Mocassin.Model.Basic
{
    /// <summary>
    ///     Abstract base class for event handlers that handle object list reindex operations provided by the specified event
    ///     port
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    internal abstract class ObjectIndexingChangedEventHandler<T1, T2, T3> : ModelEventHandler<T1, T2, T3>
        where T1 : IModelEventPort
        where T2 : ModelData
        where T3 : ModelEventManager
    {
        /// <inheritdoc />
        protected ObjectIndexingChangedEventHandler(IModelProject modelProject, DataAccessorSource<T2> dataAccessorSource, T3 eventManager)
            : base(modelProject, dataAccessorSource, eventManager)
        {
        }

        /// <inheritdoc />
        public override IDisposable SubscribeToEvent(T1 eventPort) => eventPort.WhenModelIndexingChanged.Subscribe(ProcessEvent);
    }
}