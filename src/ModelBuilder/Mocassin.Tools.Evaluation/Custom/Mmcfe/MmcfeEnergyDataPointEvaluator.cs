﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Mocassin.Framework.SQLiteCore;
using Mocassin.Mathematics.Comparer;
using Mocassin.Tools.Evaluation.Helper;
using Mocassin.Tools.Evaluation.PlotData;

namespace Mocassin.Tools.Evaluation.Custom.Mmcfe
{
    /// <summary>
    ///     An energy hyper surface evaluator that provides evaluation access to multi-doping multi-temperature MMCFE result
    ///     databases
    /// </summary>
    public class MmcfeEnergyDataPointEvaluator : IDisposable
    {
        private IReadOnlyList<MmcfeEnergyDataPoint> dataPoints;

        /// <summary>
        ///     Get the <see cref="MmcfeEnergyEvaluator" /> used by the system
        /// </summary>
        private MmcfeEnergyEvaluator EnergyEvaluator { get; }

        /// <summary>
        ///     Get a boolean flag if the <see cref="DataContext" /> should be disposed if the object is disposed
        /// </summary>
        private bool IsDataSourceDisposeWithObject { get; }

        /// <summary>
        ///     Get the <see cref="IDataContext" /> that provides the evaluation data
        /// </summary>
        private IDataContext DataContext { get; }

        /// <summary>
        ///     Get a <see cref="IReadOnlyList{T}" /> of all <see cref="MmcfeEnergyDataPoint" /> entries. Getting this value will
        ///     force load all data points
        /// </summary>
        public IReadOnlyList<MmcfeEnergyDataPoint> DataPoints => dataPoints ??= SelectEnergyDataPoints().ToList();

        /// <summary>
        ///     Creates a new <see cref="MmcfeEnergyDataPointEvaluator" /> using the provided
        ///     <see cref="IDataContext" /> with an optional flag
        /// </summary>
        /// <param name="dataContext"></param>
        /// <param name="isDataSourceDisposeWithObject"></param>
        public MmcfeEnergyDataPointEvaluator(IDataContext dataContext, bool isDataSourceDisposeWithObject = true)
        {
            DataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
            IsDataSourceDisposeWithObject = isDataSourceDisposeWithObject;
            EnergyEvaluator = new MmcfeEnergyEvaluator();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (IsDataSourceDisposeWithObject) (DataContext as IDisposable)?.Dispose();
        }

        /// <summary>
        ///     Causes the evaluator to load all data points into memory
        /// </summary>
        public void LoadDataPoints()
        {
            dataPoints = SelectEnergyDataPoints().ToList();
        }

        /// <summary>
        ///     Selects a set of <see cref="MmcfeEnergyState" /> entries from the data source based on a predicate expression
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public IQueryable<MmcfeLogEnergyEntry> SelectEnergyEntries(Expression<Func<MmcfeLogEnergyEntry, bool>> predicate)
        {
            return DataContext.Set<MmcfeLogEnergyEntry>()
                             .Where(predicate)
                             .Include(x => x.LogEntry)
                             .Include(x => x.LogEntry.MetaEntry);
        }

        /// <summary>
        ///     Selects the full set of <see cref="MmcfeEnergyState" /> entries from the data source
        /// </summary>
        /// <returns></returns>
        public IQueryable<MmcfeLogEnergyEntry> SelectEnergyEntries()
        {
            return SelectEnergyEntries(x => x.LogEntryId >= 1);
        }

        /// <summary>
        ///     Selects a set of <see cref="MmcfeEnergyDataPoint" /> entries from the data source based on a predicate expression
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public IEnumerable<MmcfeEnergyDataPoint> SelectEnergyDataPoints(Expression<Func<MmcfeLogEnergyEntry, bool>> predicate)
        {
            return SelectEnergyEntries(predicate)
                   .AsEnumerable()
                   .GroupBy(x => new {x.LogEntry.MetaEntry.CollectionIndex, x.LogEntry.MetaEntry.ConfigIndex, x.Alpha},
                       y => new {entry = y, y.LogEntry.MetaEntry})
                   .Select(x => CreateDataPoint(x, y => y.MetaEntry, y => y.entry));
        }

        /// <summary>
        ///     Create a a two level dictionary grouping based on doping information and then temperature value subgroups
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, IDictionary<double, MmcfeEnergyDataPoint>> GroupDataPointsByDopingByTemperature(
            IEqualityComparer<double> temperatureComparer = null)
        {
            temperatureComparer ??= NumericComparer.CreateRangedCombined();
            return DataPoints
                   .GroupBy(x => x.MetaEntry.DopingInfo)
                   .ToDictionary(x => x.Key,
                       y => (IDictionary<double, MmcfeEnergyDataPoint>) y.ToDictionary(arg => arg.EnergyState.Temperature, arg => arg, temperatureComparer));
        }

        /// <summary>
        ///     Create a two level dictionary grouping based on temperature information and then doping value subgroups
        /// </summary>
        /// <returns></returns>
        public IDictionary<double, IDictionary<string, MmcfeEnergyDataPoint>> GroupDataPointsByTemperatureByDoping(
            IEqualityComparer<double> temperatureComparer = null)
        {
            temperatureComparer ??= NumericComparer.CreateRangedCombined();
            return DataPoints
                   .GroupBy(x => x.EnergyState.Temperature, temperatureComparer)
                   .OrderByDescending(x => x.Key)
                   .ToDictionary(x => x.Key, y => (IDictionary<string, MmcfeEnergyDataPoint>) y.ToDictionary(arg => arg.MetaEntry.DopingInfo, arg => arg),
                       temperatureComparer);
        }

        /// <summary>
        ///     Selects the full set of <see cref="MmcfeEnergyDataPoint" /> entries from the data source
        /// </summary>
        /// <returns></returns>
        public IEnumerable<MmcfeEnergyDataPoint> SelectEnergyDataPoints()
        {
            return SelectEnergyDataPoints(x => x.LogEntry.MetaEntryId >= 1);
        }

        /// <summary>
        ///     Gets a per defect <see cref="MmcfeEnergyState" /> plot information where all values are relative to entry with the
        ///     lowest defect particle count (Normalized by affiliated defect counts)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="fixedDopingValue"></param>
        /// <param name="variableDopingId"></param>
        /// <param name="defectParticleId"></param>
        /// <param name="fixedDopingId"></param>
        /// <returns></returns>
        public PlotPoints<double, MmcfeEnergyState> GetRelativeChangePerDefectPlotData2D(IDictionary<string, MmcfeEnergyDataPoint> data,
            int fixedDopingId, double fixedDopingValue, int variableDopingId, int defectParticleId)
        {
            var keySelectorFunction = GetDopingSelectorFunction(fixedDopingId, fixedDopingValue);
            var baseData = data.Where(x => keySelectorFunction(x.Key)).OrderBy(x => x.Value.ParticleCounts[defectParticleId]).ToList();
            var baseState = baseData[0].Value.EnergyState;

            var result = new PlotPoints<double, MmcfeEnergyState>(baseData.Count);
            foreach (var pair in baseData)
            {
                var dopingValue = ParseDopingValue(pair.Value.MetaEntry.DopingInfo, variableDopingId);
                var defectCount = pair.Value.ParticleCounts[defectParticleId];
                var y = pair.Value.EnergyState.AsRelative(baseState).AsPerDefect(defectCount);
                var errorY = pair.Value.EnergyStateError.AsPerDefect(defectCount);
                result.AddPoint(dopingValue, y, 0, errorY);
            }

            return result;
        }


        /// <summary>
        ///     Gets the absolute <see cref="MmcfeEnergyState" /> plot information where all values are relative to entry with the
        ///     lowest defect particle count (No normalization)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="fixedDopingValue"></param>
        /// <param name="variableDopingId"></param>
        /// <param name="defectParticleId"></param>
        /// <param name="fixedDopingId"></param>
        /// <returns></returns>
        public PlotPoints<double, MmcfeEnergyState> GetAbsoluteChangePlotData2D(IDictionary<string, MmcfeEnergyDataPoint> data,
            int fixedDopingId, double fixedDopingValue, int variableDopingId, int defectParticleId)
        {
            var keySelectorFunction = GetDopingSelectorFunction(fixedDopingId, fixedDopingValue);
            var baseData = data.Where(x => keySelectorFunction(x.Key)).OrderBy(x => x.Value.ParticleCounts[defectParticleId]).ToList();
            var baseState = baseData[0].Value.EnergyState;

            var result = new PlotPoints<double, MmcfeEnergyState>(baseData.Count);
            foreach (var pair in baseData)
            {
                var dopingValue = ParseDopingValue(pair.Value.MetaEntry.DopingInfo, variableDopingId);
                result.AddPoint(dopingValue, pair.Value.EnergyState.AsRelative(baseState), 0, pair.Value.EnergyStateError);
            }

            return result;
        }

        /// <summary>
        ///     Writes the <see cref="PlotPoints{TX,TY}" /> for a <see cref="MmcfeEnergyState" /> over concentration curve to the
        ///     provided file location. By default the first line is skipped
        /// </summary>
        /// <param name="plotData"></param>
        /// <param name="filePath"></param>
        /// <param name="entropyInKb"></param>
        /// <param name="skipLineCount"></param>
        /// <param name="doubleFormat"></param>
        public void WriteEnergyStateOverConcentrationPlotData2DToFile(PlotPoints<double, MmcfeEnergyState> plotData, string filePath, bool entropyInKb = true,
            int skipLineCount = 1, string doubleFormat = "e13")
        {
            if (File.Exists(filePath)) File.Delete(filePath);
            using var writer = File.AppendText(filePath);
            var entropyFactor = entropyInKb ? 1.0 / Equations.Constants.BoltzmannEv : 1.0;
            writer.WriteLine("c u error-u f error-f s error-s");
            foreach (var point in plotData.Skip(skipLineCount))
            {
                writer.Write(point.X.ToString(doubleFormat));
                writer.Write(" ");
                WriteValueWithError(writer, point.Y.InnerEnergy, point.ErrorY.InnerEnergy, false, doubleFormat);
                WriteValueWithError(writer, point.Y.FreeEnergy, point.ErrorY.FreeEnergy, false, doubleFormat);
                WriteValueWithError(writer, point.Y.Entropy * entropyFactor, point.ErrorY.Entropy * entropyFactor, true, doubleFormat);
            }
        }

        /// <summary>
        ///     Writes a numeric value with error to a <see cref="StreamWriter" /> using the provided format information
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="error"></param>
        /// <param name="lastColumn"></param>
        /// <param name="valueFormat"></param>
        /// <param name="separator"></param>
        private void WriteValueWithError(StreamWriter writer, double value, double error, bool lastColumn = false, string valueFormat = "e13",
            string separator = " ")
        {
            writer.Write(value.ToString(valueFormat));
            writer.Write(separator);
            writer.Write(error.ToString(valueFormat));
            writer.Write(lastColumn ? Environment.NewLine : separator);
        }

        /// <summary>
        ///     Parses a doping value for the specified id from the doping info <see cref="string" />
        /// </summary>
        /// <param name="dopingString"></param>
        /// <param name="dopingId"></param>
        /// <returns></returns>
        public double ParseDopingValue(string dopingString, int dopingId)
        {
            var str = Regex.Match(dopingString, $"{dopingId}@([^]]+)").Groups[1].Value;
            return double.Parse(str);
        }

        /// <summary>
        ///     Parses the total number of unit cells from the specified cell info string <see cref="string" />
        /// </summary>
        /// <param name="cellString"></param>
        public int ParseUnitCellCount(string cellString)
        {
            var values = cellString.Split(',');
            return int.Parse(values[0]) * int.Parse(values[1]) * int.Parse(values[2]);
        }

        /// <summary>
        ///     Get a <see cref="Func{T1, TResult}" /> selector function that returns true if the provided oping id is at the
        ///     specified value
        /// </summary>
        /// <param name="dopingId"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Func<string, bool> GetDopingSelectorFunction(int dopingId, double value)
        {
            var str = $"[{dopingId}@{value}]";
            return x => x.Contains(str);
        }

        /// <summary>
        ///     Converts a <see cref="IGrouping{TKey,TElement}" /> of <see cref="MmcfeLogEnergyEntry" /> belonging to the same
        ///     <see cref="MmcfeLogMetaEntry" /> into a <see cref="MmcfeEnergyDataPoint" />
        /// </summary>
        /// <param name="grouping"></param>
        /// <param name="sel1"></param>
        /// <param name="sel2"></param>
        /// <returns></returns>
        protected MmcfeEnergyDataPoint CreateDataPoint<T1, T2>(IGrouping<T1, T2> grouping, Func<T2, MmcfeLogMetaEntry> sel1, Func<T2, MmcfeLogEnergyEntry> sel2)
        {
            var data = grouping.ToList();
            var (energyState, energyStateError) = EnergyEvaluator.AverageWithSem(data.Select(x => sel2(x).AsStruct()));
            return new MmcfeEnergyDataPoint(data.Count, energyState, energyStateError, sel1(data[0]));
        }
    }
}