using NroIpDatabaseGenerator.RIR.Package.Reader;
using System;
using System.Collections.Generic;
using System.Text;

namespace NroIpDatabaseGenerator.RIR.Package
{
    internal class LACNICv1PackageEntry : RirPackageEntry
    {
        public LACNICv1PackageEntry(System.IO.FileInfo file, string identifier, RirPackageEntryContentType type) : base(file, identifier, type)
        { }

        public override RirFileReader Open()
        {
            return new LACNICv1FileReader(LocalFile);
        }
    }
}
