﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Mocassin.Framework.Collections.Mocassin.Tools.Evaluation.Queries;

namespace Mocassin.Tools.Evaluation.Queries
{
    /// <summary>
    ///     Represents an evaluation of a <see cref="IEvaluableJobSet" /> that can provide resuluts
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IJobEvaluation<T> : IReadOnlyList<T>
    {
        /// <summary>
        ///     Get the <see cref="IEvaluableJobSet" /> that serves as the data source
        /// </summary>
        IEvaluableJobSet JobSet { get; }

        /// <summary>
        ///     Get the <see cref="IReadOnlyList{T}" /> of results
        /// </summary>
        ReadOnlyList<T> Result { get; }

        /// <summary>
        ///     Get the query result task or generates and invokes the task if required
        /// </summary>
        /// <returns></returns>
        Task<ReadOnlyList<T>> Run();
    }
}