using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NroIpDatabaseGenerator.SQL
{
    internal class SQLiteDriverConfiguration
    {
        public SQLiteDriverConfiguration(string databaseFilename)
        {
            DatabaseFilename = databaseFilename ?? throw new ArgumentNullException(nameof(databaseFilename));
        }

        public string DatabaseFilename { get; set; }
        public FileInfo DatabaseFile => new FileInfo(DatabaseFilename);
        public string Password { get; set; }
        public string TablePrefix { get; set; } = "";
        public string TableName { get; set; } = "iptable";
    }
}
