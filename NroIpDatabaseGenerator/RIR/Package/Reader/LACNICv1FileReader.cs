using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NroIpDatabaseGenerator.RIR.Package.Reader
{
    internal class LACNICv1FileReader : RirFileReader
    {
        public LACNICv1FileReader(FileInfo file) : base(file)
        {
        }

        public override string RirIdentifierString => "lacnic";
    }
}
