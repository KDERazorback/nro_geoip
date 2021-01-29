using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NroIpDatabaseGenerator.RIR.Package.Reader
{
    internal class APNICFileReader : RirFileReader
    {
        public APNICFileReader(FileInfo file) : base(file)
        {
        }

        public override string RirIdentifierString => "apnic";
    }
}
