using com.RazorSoftware.Logging;
using NroIpDatabaseGenerator.RIR.Package;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NroIpDatabaseGenerator.RIR
{
    internal class APNIC : RIRBase
    {
        public override string Identifier => "APNIC";

        public override string PublicFtpChecksumsUrl => "/pub/apnic/stats/apnic/%TYPE%-apnic-latest.md5";

        public override string PublicFtpHostname { get; set; } = "ftp.apnic.net";

        public override string PublicFtpStatsUrl => "/pub/apnic/stats/apnic/%TYPE%-apnic-latest";

        public override string SafeFileIdentifier => "APNIC";

        public override RirPackage DownloadPackage()
        {
            List<RirPackageEntryAssociation> databases = new List<RirPackageEntryAssociation>();

            // Download delegated file
            databases.Add(new RirPackageEntryAssociation(
                DownloadFile(PublicFtpStatsUrl.Replace("%TYPE%", "delegated"), "APNIC-latest-delegated.db"),
                DownloadFile(PublicFtpChecksumsUrl.Replace("%TYPE%", "delegated"), "APNIC-latest-delegated.db.md5"),
                RirPackageEntryContentType.Combined));
            // No need to decompress.

            //// Download assigned file
            //databases.Add(new RirPackageEntryAssociation(
            //    DownloadFile(PublicFtpStatsUrl.Replace("%TYPE%", "assigned"), "APNIC-latest-assigned.db"),
            //    /*DownloadFile(PublicFtpChecksumsUrl.Replace("%TYPE%", "assigned"), "APNIC-latest-assigned.db.md5")*/
            //    null,
            //    RirPackageEntryContentType.Assigned));
            // No need to decompress.


            // Close FTP connection
            DisconnectServer();

            // Verify checksums
            List<RirPackageEntry> entries = new List<RirPackageEntry>();
            foreach (RirPackageEntryAssociation assoc in databases)
            {
                FileInfo apnicDb = assoc.DatabaseFile;
                FileInfo apnicDbChecksum = assoc.ChecksumFile;

                if (apnicDbChecksum != null)
                {
                    string hashString;
                    using (FileStream fs = new FileStream(apnicDb.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        MD5 hasher = MD5.Create();
                        byte[] hash = hasher.ComputeHash(fs);

                        StringBuilder str = new StringBuilder();
                        for (int i = 0; i < hash.Length; i++)
                            str.Append(hash[i].ToString("X2"));

                        hashString = str.ToString();
                    }

                    string expectedHashString;
                    using (FileStream fs = new FileStream(apnicDbChecksum.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (StreamReader reader = new StreamReader(fs))
                    {
                        string[] hashParts = reader.ReadLine().Split('=', StringSplitOptions.RemoveEmptyEntries);
                        if (hashParts.Length != 2)
                        {
                            Log.WriteLine("Unexpected checksum format on file %@. Line dont have the required segment count.", LogLevel.Error, apnicDbChecksum.Name);
                            throw new InvalidDataException("Unexpected checksum file format.");
                        }

                        expectedHashString = hashParts[1].Trim();
                    }

                    if (!string.Equals(hashString, expectedHashString, StringComparison.OrdinalIgnoreCase))
                    {
                        Log.WriteLine("File verification failed for local file %@. Expected %@ but got %@.", LogLevel.Error, apnicDb.Name, expectedHashString, hashString);
                        throw new IOException("File verification failed for local file.");
                    }

                    Log.WriteLine("File verification succeeded for local file %@. Hash %@.", LogLevel.Debug, apnicDb.Name, hashString);
                }
                else
                    Log.WriteLine("Skipping verification for local file %@ because its hash file is not present on the FTP server.", LogLevel.Warning, apnicDb.Name);

                entries.Add(new APNICPackageEntry(apnicDb, "LATEST_" + assoc.Type.ToString().ToUpperInvariant(), assoc.Type));
            }

            return new RirPackage(entries.ToArray(), Identifier, DateTime.Now);
        }
    }
}

