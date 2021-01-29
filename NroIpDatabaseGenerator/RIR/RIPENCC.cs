using com.RazorSoftware.Logging;
using NroIpDatabaseGenerator.RIR.Package;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NroIpDatabaseGenerator.RIR
{
    internal class RIPENCC : RIRBase
    {
        public override string Identifier => "RIPENCC";

        public override string PublicFtpChecksumsUrl => "/pub/stats/ripencc/delegated-ripencc-latest.md5";

        public override string PublicFtpHostname { get; set; } = "ftp.ripe.net";

        public override string PublicFtpStatsUrl => "/pub/stats/ripencc/delegated-ripencc-latest";

        public override string SafeFileIdentifier => "RIPENCC";

        public override RirPackage DownloadPackage()
        {
            // Download primary file
            FileInfo ripenccDb = DownloadFile(PublicFtpStatsUrl, "RIPENCC-latest.db");
            // No need to decompress.

            FileInfo ripenccDbChecksum = DownloadFile(PublicFtpChecksumsUrl, "RIPENCC-latest.db.md5");
            // No need to decompress.

            // Close FTP connection
            DisconnectServer();

            // Verify checksums
            string hashString;
            using (FileStream fs = new FileStream(ripenccDb.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                MD5 hasher = MD5.Create();
                byte[] hash = hasher.ComputeHash(fs);

                StringBuilder str = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                    str.Append(hash[i].ToString("X2"));

                hashString = str.ToString();
            }

            string expectedHashString;
            using (FileStream fs = new FileStream(ripenccDbChecksum.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (StreamReader reader = new StreamReader(fs))
            {
                string[] hashParts = reader.ReadLine().Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (hashParts.Length != 2)
                {
                    Log.WriteLine("Unexpected checksum format on file %@. Line dont have the required segment count.", LogLevel.Error, ripenccDbChecksum.Name);
                    throw new InvalidDataException("Unexpected checksum file format.");
                }

                expectedHashString = hashParts[1].Trim();
            }

            if (!string.Equals(hashString, expectedHashString, StringComparison.OrdinalIgnoreCase))
            {
                Log.WriteLine("File verification failed for local file %@. Expected %@ but got %@.", LogLevel.Error, ripenccDb.Name, expectedHashString, hashString);
                throw new IOException("File verification failed for local file.");
            }

            Log.WriteLine("File verification succeeded for local file %@. Hash %@.", LogLevel.Debug, ripenccDb.Name, hashString);

            RirPackageEntry primaryEntry = new RIPENCCPackageEntry(ripenccDb, "LATEST_COMBINED", RirPackageEntryContentType.Combined);
            return new RirPackage(primaryEntry, Identifier, DateTime.Now);
        }
    }
}
