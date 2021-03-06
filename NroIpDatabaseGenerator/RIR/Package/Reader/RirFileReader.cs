﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NroIpDatabaseGenerator.RIR.Package.Reader
{
    internal abstract class RirFileReader : IDisposable
    {
        public abstract string RirIdentifierString { get; }
        public virtual RirIpRecord NextEntry()
        {
            string line;
            while (true)
            {
                BackendLineIndex++;

                if (BackendReader.EndOfStream)
                    return null; // End of stream

                line = BackendReader.ReadLine();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                line = line.Trim();

                try
                {
                    if (line[0] == '#' || line[0] == ';')
                        continue; // Skip comment

                    string[] parts = line.Split('|', StringSplitOptions.None);

                    if (HeaderLineIndex == -1)
                    {
                        // Check header
                        if (parts[0].Trim() != "2" || !string.Equals(parts[1].Trim(), RirIdentifierString, StringComparison.OrdinalIgnoreCase))
                            throw new RirFileReaderException(line, BackendLineIndex, "No valid header found.");

                        HeaderLineIndex = BackendLineIndex;
                        continue;
                    }

                    if (parts.Length < 6)
                        throw new RirFileReaderException(line, BackendLineIndex, "Not a valid record.");

                    if (!string.Equals(parts[0].Trim(), RirIdentifierString, StringComparison.OrdinalIgnoreCase))
                        throw new RirFileReaderException(line, BackendLineIndex, "Unexpected record start organizational unit.");

                    if (string.Equals(parts[5].Trim(), "summary", StringComparison.OrdinalIgnoreCase))
                        continue; // Skip summary line

                    if (parts.Length < 7)
                        throw new RirFileReaderException(line, BackendLineIndex, "Not a valid record.");

                    if (string.Equals(parts[2].Trim(), "asn", StringComparison.OrdinalIgnoreCase))
                        continue; // Skip ASN record

                    if (string.Equals(parts[2].Trim(), "ipv6", StringComparison.OrdinalIgnoreCase))
                        continue; // Skip ipv6 record

                    if (string.Equals(parts[2].Trim(), "ipv4", StringComparison.OrdinalIgnoreCase))
                    {
                        // Process ipv4 record
                        DateTime createdAt = DateTime.MinValue;
                        if (int.Parse(parts[5].Trim()) >= 19500101)
                            createdAt = DateTime.ParseExact(parts[5].Trim(), "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
                        RirIpRecord entry = new RirIpRecord(Settings.IsoCountryCodes, parts[1].Trim(), parts[3].Trim(), long.Parse(parts[4].Trim()), createdAt, parts[6].Trim());

                        return entry;
                    }

                    throw new RirFileReaderException(line, BackendLineIndex, "Unknown row on database file.");
                }
                catch (RirFileReaderException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new RirFileReaderException(line, BackendLineIndex, ex.Message, ex);
                }
            }
        }
        public virtual long Position => BackendStream.Position;
        public virtual void Dispose()
        {
            BackendReader?.Dispose();
            BackendStream?.Dispose();

            BackendReader = null;
            BackendStream = null;
        }
        protected FileStream BackendStream { get; set; }
        protected StreamReader BackendReader { get; set; }
        protected long BackendLineIndex { get; set; } = -1;
        protected long HeaderLineIndex { get; set; } = -1;

        protected RirFileReader(FileInfo file)
        {
            BackendStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            BackendReader = new StreamReader(BackendStream, System.Text.Encoding.ASCII, leaveOpen: true);
        }

        protected RirFileReader()
        {

        }
    }
}
