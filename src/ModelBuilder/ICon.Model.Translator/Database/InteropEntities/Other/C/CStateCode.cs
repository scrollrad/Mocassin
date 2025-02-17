﻿using System.Runtime.InteropServices;

#pragma warning disable 1591

namespace Mocassin.Model.Translator
{
    /// <summary>
    ///     C state code object. Layout marshals to its unmanaged 'C' counterpart
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 8)]
    public struct CStateCode
    {
        [field: MarshalAs(UnmanagedType.I8)]
        public long Code { get; set; }
    }
}