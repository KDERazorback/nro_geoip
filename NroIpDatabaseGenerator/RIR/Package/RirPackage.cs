using com.RazorSoftware.Logging;
using NroIpDatabaseGenerator.RIR.Package.Reader;
using System;
using System.Collections.Generic;
using System.Text;

namespace NroIpDatabaseGenerator.RIR.Package
{
    internal class RirPackage
    {
        public long TotalRecordCount 
        {
            get
            {
                long totalCount = 0;
                foreach (RirPackageEntry entry in Entries)
                {
                    if (entry.IsScanned && entry.RecordCount >= 0)
                        totalCount += entry.RecordCount;
                }

                return totalCount;
            }
        }

        public int EntryCount => Entries.Length;

        public bool IsFullyScanned
        {
            get
            {
                foreach (RirPackageEntry entry in Entries)
                {
                    if (!entry.IsScanned)
                        return false;
                }

                return true;
            }
        }

        public RirPackageEntry[] Entries { get; protected set; }
        public string RirName { get; }
        public DateTime TimeStamp { get; }

        public RirPackage(RirPackageEntry[] entries, string rirName, DateTime timeStamp)
        {
            Entries = entries;
            RirName = rirName;
            TimeStamp = timeStamp;
        }

        public RirPackage(RirPackageEntry entry, string rirName, DateTime timeStamp)
        {
            Entries = new RirPackageEntry[] { entry };
            RirName = rirName;
            TimeStamp = timeStamp;
        }

        /// <summary>
        /// Checks each entry inside the package and throws an error if any record is damaged.
        /// </summary>
        /// <param name="errorsAsWarnings">True to continue in case of errors, otherwise false</param>
        public virtual void ScanPackage(bool errorsAsWarnings)
        {
            Log.WriteLine("Pre-Scanning package for RIR %@ with %@ entries...", LogLevel.Message, RirName, EntryCount.ToString("N0"));

            Func<string, RirFileReaderException, bool> onErrorFunc = (input, ex) =>
            {
                if (errorsAsWarnings)
                {
                    Log.WriteLine("An error occurred while processing line %@. %@. Ignoring...", LogLevel.Warning, ex.LineNumber.ToString("N0"), ex.Message);
                    return true; // Handled
                }
                else
                {
                    Log.WriteLine("An error occurred while processing line %@. %@. Aborting!", LogLevel.Error, ex.LineNumber.ToString("N0"), ex.Message);
                    return false; // Not handled
                }
            };

            foreach (RirPackageEntry entry in Entries)
            {
                if (entry.IsScanned)
                {
                    Log.WriteLine("Entry %@ for RIR package %@ is already scanned. %@ records. Skipping...", LogLevel.Debug, entry.Identifier, RirName, entry.RecordCount.ToString("N0"));
                    continue;
                }

                Log.WriteLine("Pre-Scanning entry %@ for RIR Package %@...", LogLevel.Debug, entry.Identifier, RirName);
                entry.Scan(onErrorFunc);
                Log.WriteLine("Pre-Scanning found %@ records for entry %@ inside RIR Package %@...", LogLevel.Debug, entry.RecordCount.ToString("N0"), entry.Identifier, RirName);
            }

            Log.WriteLine("Pre-Scanning completed for RIR Package %@. %@ total records.", LogLevel.Debug, RirName, TotalRecordCount.ToString("N0"));
        }
    }
}
