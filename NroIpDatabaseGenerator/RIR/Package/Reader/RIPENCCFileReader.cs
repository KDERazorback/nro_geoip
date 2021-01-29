using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NroIpDatabaseGenerator.RIR.Package.Reader
{
    internal class RIPENCCFileReader : RirFileReader
    {
        public RIPENCCFileReader(FileInfo file) : base(file)
        {
        }

        public override string RirIdentifierString => "ripencc";
    }
}
