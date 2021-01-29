using NroIpDatabaseGenerator.RIR.Package.Reader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NroIpDatabaseGenerator.RIR.Package
{
    internal abstract class RirPackageEntry
    {
        public FileInfo LocalFile { get; }
        public string Identifier { get; }
        public RirPackageEntryContentType Type { get; }
        public abstract RirFileReader Open();
        public bool IsScanned { get; protected set; }
        public long RecordCount { get; protected set; }
        protected RirPackageEntry(FileInfo file, string identifier, RirPackageEntryContentType type)
        {
            LocalFile = file;
            Identifier = identifier;
            Type = type;
        }

        public virtual void Scan(Func<string, RirFileReaderException, bool> onExceptionMethod)
        {
            Func<string, RirFileReaderException, bool> OnException = (line, ex) =>
            {
                bool result = false;
                if (onExceptionMethod != null)
                    result = onExceptionMethod.Invoke(line, ex);

                return result;
            };

            long recordCount = 0;
            using (RirFileReader reader = Open())
            {
                RirIpRecord entry = null;
                while (true)
                {
                    try
                    {
                        entry = reader.NextEntry();

                        if (entry == null)
                            break;

                        recordCount++;
                    }
                    catch (RirFileReaderException ex)
                    {
                        bool handlerResult = OnException.Invoke(ex.InputString, ex);

                        if (!handlerResult)
                            throw;
                    }
                }
            }

            RecordCount = recordCount;
            IsScanned = true;
        }
    }
}
