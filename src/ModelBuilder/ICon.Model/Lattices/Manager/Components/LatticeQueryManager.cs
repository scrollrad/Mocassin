﻿using Mocassin.Model.Basic;

namespace Mocassin.Model.Lattices
{
    /// <summary>
    ///     Basic implementation of the lattice query manager that handles safe data queries and service requests to the
    ///     Lattice manager from outside sources
    /// </summary>
    internal class LatticeQueryManager : ModelQueryManager<LatticeModelData, ILatticeDataPort, LatticeDataCache, ILatticeCachePort>, ILatticeQueryPort
    {
        /// <summary>
        ///     Create new lattice query manager from data, cache object and data access locker
        /// </summary>
        /// <param name="modelData"></param>
        /// <param name="extendedData"></param>
        /// <param name="lockSource"></param>
        public LatticeQueryManager(LatticeModelData modelData, LatticeDataCache extendedData, AccessLockSource lockSource)
            : base(modelData, extendedData, lockSource)
        {
        }
    }
}