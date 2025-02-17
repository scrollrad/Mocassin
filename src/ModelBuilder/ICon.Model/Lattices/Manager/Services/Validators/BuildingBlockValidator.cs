﻿using System.Linq;
using Mocassin.Framework.Messaging;
using Mocassin.Framework.Operations;
using Mocassin.Model.Basic;
using Mocassin.Model.ModelProject;
using Mocassin.Model.Structures;

namespace Mocassin.Model.Lattices.Validators
{
    /// <summary>
    ///     Validator for new BuildingBlock model objects that checks for consistency and compatibility with existing data and
    ///     general object constraints
    /// </summary>
    public class BuildingBlockValidator : DataValidator<IBuildingBlock, MocassinLatticeSettings, ILatticeDataPort>
    {
        /// <summary>
        ///     Creates new validator with the provided project services, settings object and data reader
        /// </summary>
        /// <param name="modelProject"></param>
        /// <param name="settings"></param>
        /// <param name="dataReader"></param>
        public BuildingBlockValidator(IModelProject modelProject, MocassinLatticeSettings settings,
            IDataReader<ILatticeDataPort> dataReader)
            : base(modelProject, settings, dataReader)
        {
        }

        /// <summary>
        ///     Validate a new BuildingBlock object in terms of consistency and compatibility with existing data
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override IValidationReport Validate(IBuildingBlock obj)
        {
            var report = new ValidationReport();
            AddOccupationValidation(obj, report);
            return report;
        }

        /// <summary>
        ///     Validate matching particles and unit cell positions
        /// </summary>
        /// <param name="buildingBlock"></param>
        /// <param name="report"></param>
        protected void AddOccupationValidation(IBuildingBlock buildingBlock, ValidationReport report)
        {
            var structurePort = ModelProject.Manager<IStructureManager>().DataAccess;

            var occupationList = structurePort.Query(port => port.GetExtendedIndexToPositionList());

            for (var i = 0; i < occupationList.Count; i++)
            {
                if (occupationList[i].IsValidAndUnstable() && buildingBlock.CellEntries[i].IsVoid) continue;

                if (occupationList[i].OccupationSet.GetParticles().Contains(buildingBlock.CellEntries[i]))
                    continue;

                var detail0 = "A Particle cannot be placed at the specified position";
                report.AddWarning(WarningMessage.CreateCritical(this, detail0));
            }
        }
    }
}