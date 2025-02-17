﻿using System;
using System.Linq;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Mocassin.Framework.SQLiteCore
{
    /// <summary>
    ///     An abstract SQLite EFCore context class that supports the ICon context provider system and ensures that the
    ///     database is created
    /// </summary>
    public abstract class SqLiteContext : DbContext
    {
        /// <summary>
        ///     The total file string parameter passed to the options builder to find the database
        /// </summary>
        public string OptionsBuilderParameterString { get; internal set; }

        /// <summary>
        ///     Get or set the file name <see cref="string" /> of the database
        /// </summary>
        public string FileName { get; internal set; }

        /// <summary>
        ///     Creates a new context with the provided options builder string parameter and ensures that the database is created
        /// </summary>
        /// <param name="optionsBuilderParameterString"></param>
        protected SqLiteContext(string optionsBuilderParameterString)
        {
            OptionsBuilderParameterString = optionsBuilderParameterString;
        }

        /// <inheritdoc />
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(OptionsBuilderParameterString);
        }

        /// <summary>
        ///     Get a <see cref="ReadOnlyDbContext" /> of the current
        /// </summary>
        /// <returns></returns>
        public ReadOnlyDbContext AsReadOnly() => new ReadOnlyDbContext(this);

        /// <summary>
        ///     Creates a new generic <see cref="DbContext" /> of using the provided file path and ensures
        ///     that the database is drop-created if requested (Note: No overwrite warning is provided!)
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="dropCreate"></param>
        /// <returns></returns>
        public static TContext OpenDatabase<TContext>(string filePath, bool dropCreate = false) where TContext : SqLiteContext
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));

            var context = (TContext) Activator.CreateInstance(typeof(TContext), $"Filename={filePath}");
            context.FileName = filePath;

            if (dropCreate)
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
            }

            if (!context.GetService<IRelationalDatabaseCreator>().Exists())
                throw new InvalidOperationException("Requested database does not exist.");

            return context;
        }

        /// <summary>
        ///     Opens an existing <see cref="DbContext"/> or creates the database if required
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static TContext OpenOrCreateDatabase<TContext>(string filePath) where TContext : SqLiteContext
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));

            var context = (TContext) Activator.CreateInstance(typeof(TContext), $"Filename={filePath}");
            context.FileName = filePath;

            if (context.GetService<IRelationalDatabaseCreator>().Exists()) return context;
            context.Dispose();
            return OpenDatabase<TContext>(filePath, true);
        }

        /// <summary>
        ///     Detaches all entities from the context change tracking system
        /// </summary>
        public void DetachAllEntities()
        {
            foreach (var entityEntry in ChangeTracker.Entries().ToList()) entityEntry.State = EntityState.Detached;
        }

        /// <summary>
        ///     Executes a text command on the database
        /// </summary>
        /// <param name="commandText"></param>
        public void ExecuteCommand(string commandText)
        {
            using var connection = Database.GetDbConnection();
            try
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = commandText;
                command.ExecuteNonQuery();
            }
            finally
            {
                connection.Close();
            }
        }

        /// <summary>
        ///     Cleans the sqlite database
        /// </summary>
        public void Clean()
        {
            ExecuteCommand("vacuum main");
        }

        /// <summary>
        ///     Creates a new generic <see cref="DbContext" /> for the provided database filepath and returns a
        ///     <see cref="ReadOnlyDbContext" /> wrapper for it
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static ReadOnlyDbContext OpenDatabaseAsReadOnly<TContext>(string filePath) where TContext : SqLiteContext =>
            OpenDatabase<TContext>(filePath).AsReadOnly();
    }
}