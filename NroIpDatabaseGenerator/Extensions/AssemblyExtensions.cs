using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace NroIpDatabaseGenerator.Extensions
{
    internal static class AssemblyExtensions
    {
        public static string GetManifestResourceString(this Assembly assembly, string resourceName)
        {
            string output = null;
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                output = reader.ReadToEnd();
            }

            return output;
        }
    }
}
