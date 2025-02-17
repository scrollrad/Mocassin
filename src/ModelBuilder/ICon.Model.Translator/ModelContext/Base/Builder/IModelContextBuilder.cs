﻿using System.Threading.Tasks;

namespace Mocassin.Model.Translator.ModelContext
{
    /// <summary>
    ///     Generic awaitable model context builder for asynchronous creation of extended model data context objects
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    public interface IModelContextBuilder<TContext> where TContext : class
    {
        /// <summary>
        ///     Get the currently active build task
        /// </summary>
        Task<TContext> BuildTask { get; }

        /// <summary>
        ///     Builds a default context from the currently linked project reference data
        /// </summary>
        Task<TContext> BuildContext();

        /// <summary>
        ///     Builds all link dependent components on the context
        /// </summary>
        /// <returns></returns>
        Task BuildLinkDependentComponents();

        /// <summary>
        ///     Rebuilds on the passed model context instead of creating a new one
        /// </summary>
        /// <param name="modelContext"></param>
        Task<TContext> RebuildContext(TContext modelContext);

        /// <summary>
        ///     Checks if the requirements to build non link dependent components are met
        /// </summary>
        /// <returns></returns>
        bool CheckBuildRequirements();

        /// <summary>
        ///     Checks if the requirements to build the link dependent components are met
        /// </summary>
        /// <returns></returns>
        bool CheckLinkDependentBuildRequirements();
    }
}