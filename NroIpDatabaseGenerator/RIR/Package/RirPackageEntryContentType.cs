using System;
using System.Collections.Generic;
using System.Text;

namespace NroIpDatabaseGenerator.RIR.Package
{
    [Flags]
    public enum RirPackageEntryContentType : byte
    {
        Unknown = 0,
        Delegated = 1,
        Assigned = 2,
        Available = 4,
        Reserved = 8,

        // Combinations
        Combined = 15,

        // Aliases
        Allocated = 1,
    }
}
