﻿namespace Mocassin.Framework.SQLiteCore
{
    /// <summary>
    ///     Generic context provider for an SQLite database context
    /// </summary>
    public interface ISqLiteContextSource<out T1> where T1 : SqLiteContext
    {
        /// <summary>
        ///     Factory method that creates new context with the stored context parameter information
        /// </summary>
        /// <returns></returns>
        T1 CreateContext();
    }
}