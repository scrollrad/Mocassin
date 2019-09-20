//////////////////////////////////////////
// Project: C Monte Carlo Simulator		//
// File:	MmcFERoutine.h        		//
// Author:	Sebastian Eisele			//
//			Workgroup Martin, IPC       //
//			RWTH Aachen University      //
//			© 2018 Sebastian Eisele     //
// Short:   MMC free energy routine     //
//////////////////////////////////////////

#pragma once

#include "ExternalLibraries/sqlite3.h"
#include "Extensions/Interface/IMocsimExtension.h"
#include "Simulator/Logic/Routines/Main/MainRoutines.h"
#include "Simulator/Logic/Routines/Tracking/TransitionTracking.h"

/* Routine type definitions */

// Type for storage of MMCFE routine parameters
// Layout@ggc_x86_64 => 64@[8,8,8,8,8,8,8,8]
typedef struct MmcfeParams
{
    // The size of the energy sampling histogram
    int32_t HistogramSize;

    //  The number of steps the system should divide the alpha range into
    int32_t AlphaCount;

    //  The minimum alpha value
    double  AlphaMin;

    //  The maximum alpha value
    double  AlphaMax;

    //  The current alpha value
    double  AlphaCurrent;

    //  The range of the histogram around the affiliated average energy
    double  HistogramRange;

    // The number of cycles in each relaxation phase
    int64_t RelaxPhaseCycleCount;

    //  The number of cycles in each logging phase
    int64_t LogPhaseCycleCount;

} MmcfeParams_t;

// Type for holding MMCFE log entry information
// Layout@ggc_x86_64 => 108@[24,24,56]
typedef struct MmcfeLog
{
    // The linearized state of the simulation lattice
    LatticeState_t          Lattice;

    // The state of the dynamic energy histogram
    DynamicJumpHistogram_t  Histogram;

    // The state of the parameter struct as checkpoint data
    MmcfeParams_t           ParamsState;

} MmcfeLog_t;


// Public routine start for MMCFE that accepts a simulation context as a void pointer
void MMCFE_StartRoutine(void* context);

// Opens an sqlite3 MMCFE-Log database context. The method ensures that the database is created if it doesnt exist
sqlite3* MMCFE_OpenLogDatabase(const char* dbPath);

// Adds an MMCFE log entry to the passed sqlite3 database connection
error_t MMCFE_WriteEntryToLogDb(sqlite3* db, const MmcfeLog_t*restrict logEntry);