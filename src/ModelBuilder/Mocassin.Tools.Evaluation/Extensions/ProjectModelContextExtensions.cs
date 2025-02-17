﻿using System.Collections.Generic;
using Mocassin.Mathematics.Coordinates;
using Mocassin.Model.Basic;
using Mocassin.Model.Structures;
using Mocassin.Model.Translator.ModelContext;
using Mocassin.Symmetry.Analysis;

namespace Mocassin.Tools.Evaluation.Extensions
{
    /// <summary>
    ///     Provides extension methods for extracting general information from a <see cref="IProjectModelContext" />
    /// </summary>
    public static class ProjectModelContextExtensions
    {
        /// <summary>
        ///     Get the collection of <see cref="IModelObject" /> instances of the specified type
        /// </summary>
        /// <typeparam name="TObject"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IEnumerable<TObject> GetModelObjects<TObject>(this IProjectModelContext value) where TObject : IModelObject =>
            value.ModelProject.DataTracker.Objects<TObject>();

        /// <summary>
        ///     Get a <see cref="IModelObject" /> of specified type by index
        /// </summary>
        /// <typeparam name="TObject"></typeparam>
        /// <param name="value"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static TObject GetModelObject<TObject>(this IProjectModelContext value, int index) where TObject : IModelObject =>
            value.ModelProject.DataTracker.FindObject<TObject>(index);

        /// <summary>
        ///     Get a <see cref="IModelObject" /> of specified type by key
        /// </summary>
        public static TObject GetModelObject<TObject>(this IProjectModelContext value, string key) where TObject : IModelObject =>
            value.ModelProject.DataTracker.FindObject<TObject>(key);

        /// <summary>
        ///     Gets the <see cref="IUnitCellProvider{T1}" /> for the context
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IUnitCellProvider<ICellSite> GetUnitCellProvider(this IProjectModelContext value)
        {
            return value.ModelProject.Manager<IStructureManager>().DataAccess.Query(x => x.GetFullUnitCellProvider());
        }

        /// <summary>
        ///     Get the <see cref="IUnitCellVectorEncoder" /> for the context
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IUnitCellVectorEncoder GetUnitCellVectorEncoder(this IProjectModelContext value) => value.GetUnitCellProvider().VectorEncoder;
    }
}