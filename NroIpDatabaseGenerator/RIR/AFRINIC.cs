using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using com.RazorSoftware.Logging;
using NroIpDatabaseGenerator.RIR.Package;

namespace NroIpDatabaseGenerator.RIR
{
    internal class AFRINIC : RIRBase
    {
        public override string Identifier => "AFRINIC";

        public override string PublicFtpChecksumsUrl => "/pub/stats/afrinic/delegated-afrinic-latest.md5";

        public override string PublicFtpHostname { get; set; } = "ftp.afrinic.net";

        public override string PublicFtpStatsUrl => "/pub/stats/afrinic/delegated-afrinic-latest";

        public override string SafeFileIdentifier => "AFRINIC";

        public override RirPackage DownloadPackage()
        {
            // Download primary file
            FileInfo afrinicDb = DownloadFile(PublicFtpStatsUrl, "AFRINIC-latest.db");
            // No need to decompress.

            FileInfo afrinicDbChecksum = DownloadFile(PublicFtpChecksumsUrl, "AFRINIC-latest.db.md5");
            // No need to decompress.

            // Close FTP connection
            DisconnectServer();

            // Verify checksums
            string hashString;
            using (FileStream fs = new FileStream(afrinicDb.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                MD5 hasher = MD5.Create();
                byte[] hash = hasher.ComputeHash(fs);

                StringBuilder str = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                    str.Append(hash[i].ToString("X2"));

                hashString = str.ToString();
            }

            string expectedHashString;
            using (FileStream fs = new FileStream(afrinicDbChecksum.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (StreamReader reader = new StreamReader(fs))
            {
                string[] hashParts = reader.ReadLine().Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (hashParts.Length != 2)
                {
                    Log.WriteLine("Unexpected checksum format on file %@. Line dont have the required segment count.", LogLevel.Error, afrinicDbChecksum.Name);
                    throw new InvalidDataException("Unexpected checksum file format.");
                }

                expectedHashString = hashParts[1].Trim();
            }

            if (!string.Equals(hashString, expectedHashString, StringComparison.OrdinalIgnoreCase))
            {
                Log.WriteLine("File verification failed for local file %@. Expected %@ but got %@.", LogLevel.Error, afrinicDb.Name, expectedHashString, hashString);
                throw new IOException("File verification failed for local file.");
            }

            Log.WriteLine("File verification succeeded for local file %@. Hash %@.", LogLevel.Debug, afrinicDb.Name, hashString);

            RirPackageEntry primaryEntry = new AFRINICPackageEntry(afrinicDb, "LATEST_COMBINED", RirPackageEntryContentType.Combined);
            return new RirPackage(primaryEntry, Identifier, DateTime.Now);
        }
    }
}
