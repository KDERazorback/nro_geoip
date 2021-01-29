using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NroIpDatabaseGenerator.Alpha2
{
    internal class IsoAlpha2CountryCodes
    {
        protected SortedDictionary<string, string> BackendDictionary { get; set; }
        public long Count => BackendDictionary.Count;
        public IsoAlpha2CountryCodes(FileInfo file)
        {
            using (FileStream fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                LoadFromStream(fs);
        }
        public IsoAlpha2CountryCodes(string data)
        {
            MemoryStream mem = new MemoryStream();
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(data);
            mem.Write(buffer, 0, buffer.Length);
            mem.Seek(0, SeekOrigin.Begin);

            LoadFromStream(mem);
        }

        public string GetNameFromCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;
            return BackendDictionary[code.Trim()];
        }

        public bool ContainsCode(string code)
        {
            return BackendDictionary.ContainsKey(code);
        }

        private void LoadFromStream(Stream dataStream)
        {
            if (dataStream == null)
                throw new ArgumentNullException(nameof(dataStream));

            SortedDictionary<string, string> values = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (StreamReader reader = new StreamReader(dataStream, Encoding.UTF8))
            {
                string line = null;
                while (true)
                {
                    if (reader.EndOfStream)
                        break;

                    line = reader.ReadLine().Trim();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    if (line[0] == '#' || line[0] == ';')
                        continue; // Skip comment

                    StringBuilder str = new StringBuilder(line.Length);

                    bool escaped = false;
                    for (int i = 0; i < line.Length; i++)
                    {
                        char c = line[i];

                        if (c == '"')
                        {
                            escaped = !escaped;
                            continue;
                        }

                        if (c == ',' && !escaped)
                        {
                            values.Add(line.Substring(i + 1).Trim(), str.ToString().Trim());
                            break;
                        }

                        str.Append(c);
                    }
                }
            }

            BackendDictionary = values;
        }
    }
}
