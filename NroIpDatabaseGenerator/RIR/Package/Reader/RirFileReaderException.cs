using System;
using System.Collections.Generic;
using System.Text;

namespace NroIpDatabaseGenerator.RIR.Package.Reader
{
    internal class RirFileReaderException : Exception
    {
        public string InputString { get; }
        public long LineNumber { get; }

        public RirFileReaderException(string inputString, long lineNumber) : base()
        {
            InputString = inputString;
            LineNumber = lineNumber;
        }

        public RirFileReaderException(string inputString, long lineNumber, string message) : base(message)
        {
            InputString = inputString;
            LineNumber = lineNumber;
        }

        public RirFileReaderException(string inputString, long lineNumber, string message, Exception innerException) : base(message, innerException)
        {
            InputString = inputString;
            LineNumber = lineNumber;
        }
    }
}
