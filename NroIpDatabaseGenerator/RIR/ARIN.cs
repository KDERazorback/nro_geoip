using com.RazorSoftware.Logging;
using NroIpDatabaseGenerator.RIR.Package;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NroIpDatabaseGenerator.RIR
{
    internal class ARIN : RIRBase
    {
        public override string Identifier => "ARIN";

        public override string PublicFtpChecksumsUrl => "/pub/stats/arin/delegated-arin-extended-latest.md5";

        public override string PublicFtpHostname { get; set; } = "ftp.arin.net";

        public override string PublicFtpStatsUrl => "/pub/stats/arin/delegated-arin-extended-latest";

        public override string SafeFileIdentifier => "ARIN";

        public override RirPackage DownloadPackage()
        {
            // Download primary file
            FileInfo arinDb = DownloadFile(PublicFtpStatsUrl, "ARIN-extended-latest.db");
            // No need to decompress.

            FileInfo arinDbChecksum = DownloadFile(PublicFtpChecksumsUrl, "ARIN-extended-latest.db.md5");
            // No need to decompress.

            // Close FTP connection
            DisconnectServer();

            // Verify checksums
            string hashString;
            using (FileStream fs = new FileStream(arinDb.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                MD5 hasher = MD5.Create();
                byte[] hash = hasher.ComputeHash(fs);

                StringBuilder str = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                    str.Append(hash[i].ToString("X2"));

                hashString = str.ToString();
            }

            string expectedHashString;
            using (FileStream fs = new FileStream(arinDbChecksum.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (StreamReader reader = new StreamReader(fs))
            {
                string line = reader.ReadLine();

                if (line.Length < 36)
                {
                    Log.WriteLine("Unexpected checksum format on file %@. Line dont have the required length.", LogLevel.Error, arinDbChecksum.Name);
                    throw new InvalidDataException("Unexpected checksum file format.");

                }

                expectedHashString = line.Substring(0, 32).Trim();
            }

            if (!string.Equals(hashString, expectedHashString, StringComparison.OrdinalIgnoreCase))
            {
                Log.WriteLine("File verification failed for local file %@. Expected %@ but got %@.", LogLevel.Error, arinDb.Name, expectedHashString, hashString);
                throw new IOException("File verification failed for local file.");
            }

            Log.WriteLine("File verification succeeded for local file %@. Hash %@.", LogLevel.Debug, arinDb.Name, hashString);

            RirPackageEntry primaryEntry = new ARINPackageEntry(arinDb, "LATEST_COMBINED", RirPackageEntryContentType.Combined);
            return new RirPackage(primaryEntry, Identifier, DateTime.Now);
        }
    }
}
