using System;
using System.Collections.Generic;
using System.Text;

namespace NroIpDatabaseGenerator.RIR.Package
{
    internal enum RirCompressionType : byte
    {
        None = 0,
        Gzip,
        Zip,
        Deflate,
    }
}
