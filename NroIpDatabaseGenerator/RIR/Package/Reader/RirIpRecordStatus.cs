using System;
using System.Collections.Generic;
using System.Text;

namespace NroIpDatabaseGenerator.RIR.Package.Reader
{
    internal enum RirIpRecordStatus : byte
    {
        Unknown = 0,
        Assigned = 1,
        Allocated = 2,
        Reserved = 3,
    }
}
