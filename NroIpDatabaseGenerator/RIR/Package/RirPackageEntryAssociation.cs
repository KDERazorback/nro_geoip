
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NroIpDatabaseGenerator.RIR.Package
{
    internal class RirPackageEntryAssociation
    {
        public RirPackageEntryAssociation(FileInfo databaseFile, FileInfo checksumFile, RirPackageEntryContentType type)
        {
            DatabaseFile = databaseFile ?? throw new ArgumentNullException(nameof(databaseFile));
            ChecksumFile = checksumFile;
            Type = type;
        }

        public FileInfo DatabaseFile { get; }
        public FileInfo ChecksumFile { get; }
        public RirPackageEntryContentType Type { get; }
    }
}
