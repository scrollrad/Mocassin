﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Mocassin.Framework.Events;
using Mocassin.Framework.Extensions;
using Mocassin.Framework.SQLiteCore;
using Mocassin.Model.Translator;
using Mocassin.Model.Translator.Database.Entities.Other.Meta;
using Mocassin.Tools.Evaluation.Helper;

namespace Mocassin.Tools.Evaluation.Custom.Mmcfe
{
    /// <summary>
    ///     Default <see cref="IMmcfeResultImporter" /> implementation that imports from the default simulation job folder
    /// </summary>
    public class MmcfeJobFolderImporter : IMmcfeResultImporter, IDisposable
    {
        private MmcfeLogCollectionDbContext dbContext;

        /// <summary>
        ///     Get the <see cref="ReactiveEvent{TSubject}" /> for imported job notifications
        /// </summary>
        private ReactiveEvent<int> JobImportedEvent { get; }

        /// <summary>
        ///     Get the <see cref="ReactiveEvent{TSubject}" /> for messages
        /// </summary>
        private ReactiveEvent<string> MessageEvent { get; }

        /// <inheritdoc />
        public IObservable<int> JobImportedNotification => JobImportedEvent.AsObservable();

        /// <inheritdoc />
        public IObservable<string> MessageNotifications => MessageEvent.AsObservable();

        /// <summary>
        ///     Get the <see cref="string" /> path for the simulation library
        /// </summary>
        public string SimulationLibraryPath { get; }

        /// <summary>
        ///     Get the <see cref="string" /> path for the root directory that contains the job folders
        /// </summary>
        public string JobFolderRootPath { get; }

        /// <inheritdoc />
        public bool IsImporting { get; private set; }

        /// <inheritdoc />
        public bool IsSaving { get; private set; }

        /// <summary>
        ///     Get or set a boolean flag to exclude the import of binary raw data
        /// </summary>
        public bool IsExcludeRawData { get; set; }

        /// <summary>
        ///     Get or set a boolean flag if job folders are deleted as soon as they are imported into the raw database
        /// </summary>
        public bool IsDeleteJobFolders { get; set; }

        /// <summary>
        ///     Get or set the number of imports per save
        /// </summary>
        public int ImportsPerSave { get; set; }

        /// <inheritdoc />
        public MmcfeLogCollectionDbContext ImportDbContext
        {
            get => dbContext;
            set
            {
                if (IsImporting) throw new InvalidOperationException("Cannot change context while importing data.");
                dbContext = value;
            }
        }

        /// <summary>
        ///     Creates a new <see cref="MmcfeJobFolderImporter" /> using the provided path information
        /// </summary>
        /// <param name="simulationLibraryPath"></param>
        /// <param name="jobFolderRootPath"></param>
        public MmcfeJobFolderImporter(string simulationLibraryPath, string jobFolderRootPath)
        {
            SimulationLibraryPath = simulationLibraryPath ?? throw new ArgumentNullException(nameof(simulationLibraryPath));
            JobFolderRootPath = jobFolderRootPath ?? throw new ArgumentNullException(nameof(jobFolderRootPath));
            JobImportedEvent = new ReactiveEvent<int>();
            MessageEvent = new ReactiveEvent<string>();
            ImportsPerSave = 100;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            JobImportedEvent.OnCompleted();
            MessageEvent.OnCompleted();
        }

        /// <inheritdoc />
        public void Import(Expression<Func<JobMetaDataEntity, bool>> predicate = null)
        {
            predicate ??= x => true;
            if (ImportDbContext == null) throw new InvalidOperationException("Target database context is not specified.");

            var importTasks = new List<Task<IList<object>>>(ImportsPerSave);
            var saveTask = Task.CompletedTask;
            IsImporting = true;
            using (var library = OpenSimulationLibrary())
            {
                var importCount = 0;
                foreach (var jobMetaData in library.JobMetaData.Where(predicate))
                {
                    IList<object> LocalImport()
                    {
                        var items = TryImportResult(jobMetaData, out var innerException);
                        JobImportedEvent.OnNext(jobMetaData.JobModelId);
                        if (items == null)
                        {
                            MessageEvent.OnNext($"Failed to import job ({jobMetaData.JobModelId}):\n{innerException.Message}");
                        }
                        return items ?? new List<object>();
                    }

                    importTasks.Add(Task.Run(LocalImport));
                    if (++importCount < ImportsPerSave) continue;
                    saveTask = AsyncSaveAndDetachEntities(importTasks.ToList(), saveTask);
                    importTasks.Clear();
                    importCount = 0;
                }
            }

            AsyncSaveAndDetachEntities(importTasks, saveTask).Wait();
            IsImporting = false;
        }

        /// <summary>
        ///     Awaits all pending import tasks and the previous save task and then saves and detaches the current import queue
        /// </summary>
        protected virtual Task AsyncSaveAndDetachEntities(IList<Task<IList<object>>> importTasks, Task previousSaveTask)
        {
            Task.WhenAll(importTasks.Concat(previousSaveTask.AsSingleton())).Wait();
            return Task.Run(() =>
            {
                IsSaving = true;
                ImportDbContext.AddRange(importTasks.SelectMany(x => x.Result));
                ImportDbContext.SaveChanges();
                ImportDbContext.DetachAllEntities();
                importTasks.Clear();
                IsSaving = false;
            });
        }

        /// <summary>
        ///     Opens the <see cref="ISimulationLibrary" /> that provides the import meta information
        /// </summary>
        /// <returns></returns>
        protected virtual ISimulationLibrary OpenSimulationLibrary() => SqLiteContext.OpenDatabase<SimulationDbContext>(SimulationLibraryPath);

        /// <summary>
        ///     Tries to import a single MMCFE result that belongs to the passed <see cref="JobMetaDataEntity" /> and provides
        ///     possibly occured <see cref="Exception" />
        /// </summary>
        /// <param name="metaData"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        protected virtual IList<object> TryImportResult(JobMetaDataEntity metaData, out Exception exception)
        {
            exception = null;

            try
            {
                var energyEvaluator = new MmcfeEnergyEvaluator();
                var results = new List<object>(250);
                using var context = GetEvaluationContext(metaData);
                var readers = context
                              .FullReaderSet()
                              .ToList();
                var metaEntry = new MmcfeLogMetaEntry(metaData);
                var logEntries = context
                                 .LogSet()
                                 .OrderBy(x => x.Alpha)
                                 .Select(x => MmcfeExtendedLogEntry.Create(x, metaEntry, IsExcludeRawData))
                                 .ToList();
                var energyEntries = energyEvaluator
                                    .CalculateEnergyStates(readers, metaData.Temperature, MetaDataHelper.GetNumberOfUnitCells(metaEntry))
                                    .Zip(logEntries, (state, entry) => MmcfeLogEnergyEntry.Create(state, entry));

                results.Add(metaEntry);
                results.AddRange(logEntries);
                results.AddRange(energyEntries);
                foreach (var item in readers) item.Dispose();
                if (IsDeleteJobFolders) DeleteJobFolder(metaData);
                return results;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                exception = e;
                return null;
            }
        }

        /// <summary>
        ///     Gets the <see cref="MmcfeLogEvaluationContext" /> for the passed <see cref="JobMetaDataEntity" />
        /// </summary>
        /// <param name="metaData"></param>
        /// <returns></returns>
        protected virtual MmcfeLogEvaluationContext GetEvaluationContext(JobMetaDataEntity metaData)
        {
            var filePath = $"{JobFolderRootPath}//Job{metaData.JobModelId:D5}//mmcfelog.db";
            return MmcfeLogEvaluationContext.OpenFile(filePath);
        }

        /// <summary>
        ///     Deletes the job folder affiliated with the passed <see cref="JobMetaDataEntity" />
        /// </summary>
        /// <param name="metaData"></param>
        protected void DeleteJobFolder(JobMetaDataEntity metaData)
        {
            var path = $"{JobFolderRootPath}//Job{metaData.JobModelId:D5}";
            Directory.Delete(path, true);
        }

        /// <summary>
        ///     Performs the default import of MMCFE results into raw and evaluation database. Optionally Zips the raw database
        /// </summary>
        /// <param name="zipRawDatabase"></param>
        public void RunImport(bool zipRawDatabase = true)
        {
            var rawPath = $"{JobFolderRootPath}//mmcfe.raw.db";
            var evalPath = $"{JobFolderRootPath}//mmcfe.eval.db";
            using var context = SqLiteContext.OpenDatabase<MmcfeLogCollectionDbContext>(rawPath, true);
            ImportDbContext = context;
            MessageEvent.OnNext($"Starting import for '{SimulationLibraryPath}' using '{JobFolderRootPath}' as root path.");
            Import();
            MessageEvent.OnNext($"Raw import to '{rawPath}' completed, creating clean evaluation database '{evalPath}'.");
            context.CopyDatabaseWithoutRawData(evalPath);
            MessageEvent.OnNext("Eval database created.");
            if (zipRawDatabase)
            {
                var zipPath = $"{JobFolderRootPath}//mmcfe.raw.zip";
                ZipFile(rawPath, zipPath);
            }

            ImportDbContext = null;
        }

        /// <summary>
        ///     Zips the file at the source path into the target archive. Optionally deletes the raw file after completion
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <param name="targetFilePath"></param>
        /// <param name="deleteSource"></param>
        public void ZipFile(string sourceFilePath, string targetFilePath, bool deleteSource = true)
        {
            if (sourceFilePath == null) throw new ArgumentNullException(nameof(sourceFilePath));
            if (targetFilePath == null) throw new ArgumentNullException(nameof(targetFilePath));

            MessageEvent.OnNext($"Creating ZIP archive '{targetFilePath}' from '{sourceFilePath}'.");
            using (var targetStream = new FileStream(targetFilePath, FileMode.Create))
            {
                using var archive = new ZipArchive(targetStream, ZipArchiveMode.Create);
                var entry = archive.CreateEntryFromFile(sourceFilePath, Path.GetFileName(sourceFilePath));
                var sourceInfo = new FileInfo(sourceFilePath);
                var targetInfo = new FileInfo(targetFilePath);
                var message =
                    $"Archive created @ {sourceInfo.Length / 1024.0} KB ==> {targetInfo.Length / 1024.0} KB ({(double) targetInfo.Length / sourceInfo.Length} %)";
                MessageEvent.OnNext(message);
            }

            if (deleteSource) File.Delete(sourceFilePath);
        }

        /// <summary>
        ///     Counts how many entries can be imported with the current settings
        /// </summary>
        /// <returns></returns>
        public int CountImportableEntries()
        {
            using var context = OpenSimulationLibrary();
            return context.JobMetaData.Count();
        }
    }
}