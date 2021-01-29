using System;
using System.Collections.Generic;
using System.Text;

namespace NroIpDatabaseGenerator.SQL
{
    internal class MySqlDriverConfiguration
    {
        public MySqlDriverConfiguration(string hostname, string username, string password, string schema, string tablePrefix)
        {
            Hostname = hostname ?? throw new ArgumentNullException(nameof(hostname));
            Username = username ?? throw new ArgumentNullException(nameof(username));
            Password = password ?? throw new ArgumentNullException(nameof(password));
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            TablePrefix = tablePrefix ?? throw new ArgumentNullException(nameof(tablePrefix));
        }

        public string Hostname { get; set; } = "localhost";
        public string Username { get; set; }
        public string Password { get; set; }
        public string Schema { get; set; } = "geoip";
        public string TablePrefix { get; set; } = "";
        public int Port { get; set; } = 3306;
        public bool Transactional { get; set; } = true;
        public bool LockTables { get; set; } = true;
        public string TableName { get; set; } = "iptable";
    }
}
