using System;
using System.Collections.Generic;
using System.Text;
using NroIpDatabaseGenerator.RIR.Package.Reader;

namespace NroIpDatabaseGenerator.RIR.Package
{
    internal class AFRINICPackageEntry : RirPackageEntry
    {
        public AFRINICPackageEntry(System.IO.FileInfo file, string identifier, RirPackageEntryContentType type) : base(file, identifier, type)
        { }

        public override RirFileReader Open()
        {
            return new AFRINICFileReader(LocalFile);
        }
    }
}
