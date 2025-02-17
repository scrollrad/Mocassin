﻿using System.Collections.Generic;
using Mocassin.Framework.Collections.Mocassin.Tools.Evaluation.Queries;
using Mocassin.Framework.Extensions;
using Mocassin.Model.Particles;
using Mocassin.Tools.Evaluation.Context;
using Mocassin.Tools.Evaluation.Extensions;
using Mocassin.Tools.Evaluation.Helper;
using Mocassin.Tools.Evaluation.Queries.Data;

namespace Mocassin.Tools.Evaluation.Queries
{
    /// <summary>
    ///     Query to extract the the <see cref="MobileTrackerResult" /> list from a <see cref="JobContext" /> sequence
    /// </summary>
    public class MobileTrackingEvaluation : JobEvaluation<ReadOnlyList<MobileTrackerResult>>
    {
        /// <inheritdoc />
        public MobileTrackingEvaluation(IEvaluableJobSet jobSet)
            : base(jobSet)
        {
            ExecuteParallel = true;
        }

        /// <inheritdoc />
        protected override ReadOnlyList<MobileTrackerResult> GetValue(JobContext context)
        {
            var lattice = context.McsReader.ReadLattice();
            var trackerMapping = context.McsReader.ReadMobileTrackerMapping();
            var trackerData = context.McsReader.ReadMobileTrackers();
            var result = new List<MobileTrackerResult>(trackerMapping.Length);
            var vectorTransformer = context.ModelContext.GetUnitCellVectorEncoder().Transformer;

            for (var i = 0; i < trackerMapping.Length; i++)
            {
                var positionId = trackerMapping[i];
                var particle = context.ModelContext.GetModelObject<IParticle>(lattice[positionId]);
                var vector = vectorTransformer.ToCartesian(trackerData[i].AsVector());
                result.Add(new MobileTrackerResult(particle, positionId, vector * UnitConversions.Length.AngstromToMeter));
            }

            return result.AsReadOnlyList();
        }
    }
}