using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NroIpDatabaseGenerator.RIR.Package.Reader
{
    internal class AFRINICFileReader : RirFileReader
    {
        public AFRINICFileReader(FileInfo file) : base(file)
        {
        }

        public override string RirIdentifierString => "afrinic";
    }
}
