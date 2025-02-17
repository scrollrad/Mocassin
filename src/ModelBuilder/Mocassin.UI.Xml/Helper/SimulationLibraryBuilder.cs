﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Mocassin.Framework.Events;
using Mocassin.Framework.Extensions;
using Mocassin.Framework.SQLiteCore;
using Mocassin.Model.ModelProject;
using Mocassin.Model.Translator;
using Mocassin.Model.Translator.EntityBuilder;
using Mocassin.Model.Translator.Jobs;
using Mocassin.Model.Translator.ModelContext;
using Mocassin.UI.Data.Base;
using Mocassin.UI.Data.Customization;
using Mocassin.UI.Data.Jobs;
using Mocassin.UI.Data.Main;

namespace Mocassin.UI.Data.Helper
{
    /// <summary>
    ///     Build system that translates <see cref="SimulationDbBuildTemplate" /> instances into
    ///     <see cref="ISimulationLibrary" /> interfaces
    /// </summary>
    public class SimulationLibraryBuilder : IDisposable
    {
        /// <summary>
        ///     Get the maximum supported number of jobs per database
        /// </summary>
        public static int MaxJobCountPerContext { get; } = 99999;

        private readonly object lockObject = new object();
        private int BuildCounter { get; set; }

        /// <summary>
        ///     Get or set a boolean flag if the builder should save the context after the process
        /// </summary>
        public bool IsSaveAfterBuildActive { get; set; }

        /// <summary>
        ///     Get or set a boolean flag if the data should be appended to an existing database
        /// </summary>
        public bool IsAppendModeActive { get; set; }

        /// <summary>
        ///     Get or set the <see cref="CancellationToken" /> for the build process
        /// </summary>
        private CancellationToken CancellationToken { get; set; }

        /// <summary>
        ///     Get the <see cref="ReactiveEvent{TSubject}" /> for distribution of <see cref="LibraryBuildStatus" /> notifications
        /// </summary>
        private ReactiveEvent<LibraryBuildStatus> BuildStatusEvent { get; }

        /// <summary>
        ///     Get the <see cref="ReactiveEvent{TSubject}" /> for distribution of job build notifications
        /// </summary>
        private ReactiveEvent<(int Done, int Total)> JobBuildCounterEvent { get; }

        /// <summary>
        ///     Get the <see cref="IObservable{T}" /> that informs about changes in the <see cref="LibraryBuildStatus" /> and
        ///     distributes caught <see cref="Exception" />
        /// </summary>
        public IObservable<LibraryBuildStatus> LibraryBuildStatusNotifications => BuildStatusEvent.AsObservable();

        /// <summary>
        ///     Get the <see cref="IObservable{T}" /> that informs about changes in the number of successfully build job entities
        /// </summary>
        public IObservable<(int Done, int Total)> JobBuildCounterNotifications => JobBuildCounterEvent.AsObservable();

        /// <summary>
        ///     Creates a new <see cref="SimulationLibraryBuilder" />
        /// </summary>
        public SimulationLibraryBuilder()
        {
            BuildStatusEvent = new ReactiveEvent<LibraryBuildStatus>();
            JobBuildCounterEvent = new ReactiveEvent<(int Done, int Total)>();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            JobBuildCounterEvent.OnCompleted();
            BuildStatusEvent.OnCompleted();
        }


        /// <summary>
        ///     Executes a full translation cycle of a <see cref="SimulationDbBuildTemplate" /> into a
        ///     <see cref="ISimulationLibrary" /> using the passed file path <see cref="string" /> and
        ///     <see cref="IModelProject" /> interface
        /// </summary>
        /// <param name="simulationDbBuildTemplate"></param>
        /// <param name="filePath"></param>
        /// <param name="modelProject"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ISimulationLibrary BuildLibrary(SimulationDbBuildTemplate simulationDbBuildTemplate, string filePath,
            IModelProject modelProject, CancellationToken cancellationToken = default)
        {
            BuildStatusEvent.OnNext(LibraryBuildStatus.BuildProcessStarted);
            CancellationToken = cancellationToken;
            SimulationDbContext libraryContext = null;
            var isFullyBuild = false;

            try
            {
                if (CheckCancel() || !CheckCustomizationTemplateIsCompatibleToModel(simulationDbBuildTemplate.ProjectModelData, simulationDbBuildTemplate.ProjectCustomizationTemplate))
                    return null;
                if (CheckCancel() || !TryPrepareLibraryContext(filePath, out libraryContext))
                    return null;
                if (CheckCancel() || !TryPrepareModelProject(simulationDbBuildTemplate.ProjectModelData, modelProject))
                    return null;
                if (CheckCancel() || !TryPrepareModelCustomization(modelProject, simulationDbBuildTemplate.ProjectCustomizationTemplate))
                    return null;
                if (CheckCancel() || !TryBuildModelContext(modelProject, out var modelContext))
                    return null;
                if (CheckCancel() || !TryBuildLibraryContent(modelContext, libraryContext, simulationDbBuildTemplate, out var jobPackageModels))
                    return null;
                if (CheckCancel() || !TryAddBuildMetaData(jobPackageModels, simulationDbBuildTemplate))
                    return null;
                if (CheckCancel() || !TryAddContentsToLibrary(libraryContext, jobPackageModels))
                    return null;
                isFullyBuild = true;
            }
            finally
            {
                if (!isFullyBuild) libraryContext?.Dispose();
            }

            BuildStatusEvent.OnNext(LibraryBuildStatus.BuildProcessCompleted);
            return libraryContext;
        }

        /// <summary>
        ///     Checks if cancellation is requested and changes the <see cref="LibraryBuildStatus" /> flag if required
        /// </summary>
        /// <returns></returns>
        private bool CheckCancel()
        {
            if (!CancellationToken.IsCancellationRequested) return false;
            BuildStatusEvent.OnNext(LibraryBuildStatus.Cancel);
            return true;
        }

        /// <summary>
        ///     Verifies if the passed <see cref="ProjectModelData"/> is compatible to the passed <see cref="ProjectCustomizationTemplate"/>
        /// </summary>
        /// <param name="projectModelData"></param>
        /// <param name="customizationTemplate"></param>
        /// <returns></returns>
        private bool CheckCustomizationTemplateIsCompatibleToModel(ProjectModelData projectModelData, ProjectCustomizationTemplate customizationTemplate)
        {
            BuildStatusEvent.OnNext(LibraryBuildStatus.CheckingTemplates);
            var hash = projectModelData.Json.GetHashCode();
            var isOk = hash == customizationTemplate.ModelHash;
            if (!isOk) BuildStatusEvent.OnNext(LibraryBuildStatus.IncompatibleCustomizationTemplateError);
            return isOk;
        }

        /// <summary>
        ///     Tries to prepare a <see cref="IModelProject" /> using the <see cref="ProjectModelData" /> data object
        /// </summary>
        /// <param name="projectModel"></param>
        /// <param name="modelProject"></param>
        /// <returns></returns>
        private bool TryPrepareModelProject(ProjectModelData projectModel, IModelProject modelProject)
        {
            BuildStatusEvent.OnNext(LibraryBuildStatus.PreparingModelProject);

            try
            {
                modelProject.ResetProject();
                var reports = modelProject.InputPipeline.PushToProject(projectModel.GetInputSequence());
                if (reports.Any(x => !x.IsGood)) throw new InvalidOperationException("Translation of invalid model.");
                return true;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                BuildStatusEvent.OnNext(LibraryBuildStatus.ModelProjectPreparationError);
                BuildStatusEvent.OnError(exception);
                modelProject.ResetProject();
                return false;
            }
        }

        /// <summary>
        ///     Tries to prepare the customization of a prepared <see cref="IModelProject" /> using the provided
        ///     <see cref="ModelCustomizationData" />
        /// </summary>
        /// <param name="modelProject"></param>
        /// <param name="modelCustomization"></param>
        /// <returns></returns>
        private bool TryPrepareModelCustomization(IModelProject modelProject, ModelCustomizationData modelCustomization)
        {
            BuildStatusEvent.OnNext(LibraryBuildStatus.PreparingModelCustomization);

            try
            {
                modelCustomization.PushToModel(modelProject);
                return true;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                BuildStatusEvent.OnNext(LibraryBuildStatus.ModelCustomizationPreparationError);
                BuildStatusEvent.OnError(exception);
                modelProject.ResetProject();
                return false;
            }
        }

        /// <summary>
        ///     Tries to build a symmetry extended <see cref="IProjectModelContext" /> for the provided and prepared
        ///     <see cref="IModelProject" />
        /// </summary>
        /// <param name="modelProject"></param>
        /// <param name="modelContext"></param>
        /// <returns></returns>
        private bool TryBuildModelContext(IModelProject modelProject, out IProjectModelContext modelContext)
        {
            BuildStatusEvent.OnNext(LibraryBuildStatus.BuildingModelContext);
            try
            {
                var builder = new ProjectModelContextBuilder(modelProject);
                modelContext = builder.BuildContextAsync().Result;
                return true;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                BuildStatusEvent.OnNext(LibraryBuildStatus.ModelContextBuildError);
                BuildStatusEvent.OnError(exception);
                modelContext = null;
                return false;
            }
        }

        /// <summary>
        ///     Tries to prepare a new <see cref="SimulationDbContext" /> using th provided file path <see cref="string" />
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        private bool TryPrepareLibraryContext(string filePath, out SimulationDbContext dbContext)
        {
            BuildStatusEvent.OnNext(LibraryBuildStatus.PreparingLibrary);

            try
            {
                dbContext = IsAppendModeActive 
                    ? SqLiteContext.OpenOrCreateDatabase<SimulationDbContext>(filePath) 
                    : SqLiteContext.OpenDatabase<SimulationDbContext>(filePath, true);
                return true;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                BuildStatusEvent.OnNext(LibraryBuildStatus.LibraryPreparationError);
                BuildStatusEvent.OnError(exception);
                dbContext = null;
                return false;
            }
        }

        /// <summary>
        ///     Tries to build the <see cref="IList{T}" /> of <see cref="SimulationJobPackageModel" /> defined by a
        ///     <see cref="SimulationDbBuildTemplate" /> using the provided <see cref="IProjectModelContext" />
        /// </summary>
        /// <param name="modelContext"></param>
        /// <param name="dbContext"></param>
        /// <param name="dbBuildTemplate"></param>
        /// <param name="jobPackageModels"></param>
        /// <returns></returns>
        private bool TryBuildLibraryContent(IProjectModelContext modelContext, SimulationDbContext dbContext, SimulationDbBuildTemplate dbBuildTemplate, out IList<SimulationJobPackageModel> jobPackageModels)
        {
            BuildStatusEvent.OnNext(LibraryBuildStatus.BuildingLibrary);

            try
            {
                BuildCounter = 0;
                var totalJobCount = dbBuildTemplate.ProjectJobSetTemplate.GetTotalJobCount(modelContext.ModelProject);

                ThrowIfAddWouldExceedMaxJobCount(totalJobCount, dbContext);

                jobPackageModels = dbBuildTemplate.ProjectJobSetTemplate.ToInternals(modelContext.ModelProject)
                                                  .Select(jobs => GetPreparedJobBuilder(modelContext, jobs, totalJobCount)
                                                      .BuildJobPackageModel(jobs, CancellationToken))
                                                  .ToList();
                jobPackageModels.Action(model => model.Description = MakeJobPackageDescription(dbBuildTemplate)).Load();
                return true;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                BuildStatusEvent.OnNext(LibraryBuildStatus.LibraryBuildingError);
                BuildStatusEvent.OnError(exception);
                jobPackageModels = null;
                return false;
            }
        }

        /// <summary>
        ///     Tries to save an <see cref="IEnumerable{T}" /> sequence of <see cref="SimulationJobPackageModel" /> instances to
        ///     the passed <see cref="SimulationDbContext" />
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="jobPackageModels"></param>
        /// <returns></returns>
        private bool TryAddContentsToLibrary(SimulationDbContext dbContext,
            IEnumerable<SimulationJobPackageModel> jobPackageModels)
        {
            BuildStatusEvent.OnNext(LibraryBuildStatus.SavingLibraryContents);

            try
            {
                dbContext.AddRange(jobPackageModels);
                if (!IsSaveAfterBuildActive) return true;
                dbContext.SaveChanges();
                return true;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                BuildStatusEvent.OnNext(LibraryBuildStatus.LibraryContentSavingError);
                BuildStatusEvent.OnError(exception);
                return false;
            }
        }

        /// <summary>
        ///     Tries to add all meta information of the <see cref="SimulationDbBuildTemplate" /> to the prepared set of
        ///     <see cref="SimulationJobPackageModel" /> instances
        /// </summary>
        /// <param name="jobPackageModels"></param>
        /// <param name="simulationDbBuildTemplate"></param>
        /// <returns></returns>
        private bool TryAddBuildMetaData(IEnumerable<SimulationJobPackageModel> jobPackageModels, SimulationDbBuildTemplate simulationDbBuildTemplate)
        {
            BuildStatusEvent.OnNext(LibraryBuildStatus.AddingLibraryMetaData);

            try
            {
                var buildXml = simulationDbBuildTemplate.ToXml();
                var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                foreach (var packageModel in jobPackageModels)
                {
                    packageModel.ProjectXml = buildXml;
                    packageModel.ProjectGuid = simulationDbBuildTemplate.ProjectModelData.Key;
                    packageModel.ModelSystemVersion = version;
                }

                return true;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                BuildStatusEvent.OnNext(LibraryBuildStatus.MetaDataAddError);
                BuildStatusEvent.OnError(exception);
                return false;
            }
        }

        /// <summary>
        ///     Prepares a <see cref="IJobDbEntityBuilder" /> that can process the passed combination of
        ///     <see cref="IProjectModelContext" /> and <see cref="IJobCollection" />
        /// </summary>
        /// <param name="modelContext"></param>
        /// <param name="jobCollection"></param>
        /// <param name="totalJobCount"></param>
        /// <returns></returns>
        private IJobDbEntityBuilder GetPreparedJobBuilder(IProjectModelContext modelContext, IJobCollection jobCollection,
            int totalJobCount)
        {
            var builder = new JobDbEntityBuilder(modelContext);
            builder.WhenJobIsBuild.Subscribe(x =>
            {
                lock (lockObject)
                {
                    BuildCounter++;
                }

                JobBuildCounterEvent.OnNext((BuildCounter, totalJobCount));
            });

            foreach (var optimizer in jobCollection.GetPostBuildOptimizers()) builder.AddPostBuildOptimizer(optimizer);

            return builder;
        }

        /// <summary>
        ///     Creates a description string for a <see cref="SimulationJobPackageModel"/>
        /// </summary>
        /// <param name="dbBuildTemplate"></param>
        /// <returns></returns>
        private string MakeJobPackageDescription(SimulationDbBuildTemplate dbBuildTemplate)
        {
            return $"Build=[{dbBuildTemplate.Name}]";
        }

        /// <summary>
        ///     Throws if adding a number of jobs to the provided <see cref="SimulationDbContext"/> would exceed the maximum job index limitation
        /// </summary>
        /// <param name="newJobsCount"></param>
        /// <param name="simLibrary"></param>
        private void ThrowIfAddWouldExceedMaxJobCount(int newJobsCount, ISimulationLibrary simLibrary)
        {
            var totalCount = newJobsCount + simLibrary.JobModels.Count();
            if (totalCount <= MaxJobCountPerContext) return;
            throw new InvalidOperationException($"Adding all jobs exceeds the job limit of the database ({totalCount} / {MaxJobCountPerContext})!");
        }
    }
}