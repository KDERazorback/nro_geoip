using com.RazorSoftware.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using System.Data.Common;
using NroIpDatabaseGenerator.Extensions;
using System.IO;

namespace NroIpDatabaseGenerator.SQL
{
    internal class MySqlIpDatabase : IpDatabase
    {
        public MySqlDriverConfiguration Configuration { get; }
        protected new MySqlConnection Connection { get; set; }
        protected MySqlTransaction Transaction { get; set; }
        public override string TableName => Configuration.TablePrefix + Configuration.TableName;
        public bool IsConnected
        {
            get
            {
                return (Connection != null && Connection.State == System.Data.ConnectionState.Open);
            }
        }


        public MySqlIpDatabase(MySqlDriverConfiguration configuration) : base()
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));


            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder()
            {
                ApplicationName = "NroIpDatabaseGen",
                Server = configuration.Hostname,
                Port = (uint)configuration.Port,
                UserID = "**redacted**",
                Password = "**redacted**",
                Database = configuration.Schema,
                //UseCompression = true,
            };

            Log.WriteLine("Connecting to MySQL instance with connection string: %@...", LogLevel.Debug, builder.ConnectionString);
            builder.UserID = configuration.Username;
            builder.Password = configuration.Password;
            Connection = new MySqlConnection(builder.ConnectionString);
        }

        public override void Connect()
        {
            if (IsConnected)
                throw new InvalidOperationException("There is already a connection active for this instance.");

            try
            {
                Log.Write("Opening connection to %@ host at %@... ", LogLevel.Message, Configuration.TableName, Configuration.Hostname);

                Connection.Open();

                Log.WriteColoredLine("OK ! MySQL %@", ConsoleColor.Green, LogLevel.Message, Connection.ServerVersion);
            }
            catch (Exception ex)
            {
                Log.WriteColoredLine("FAIL !", ConsoleColor.Red);
                Log.WriteLine("Failed to open a connection to the target host. [%@] %@.", LogLevel.Error, ex.GetType().Name, ex.Message);
                throw;
            }
        }

        public override void Disconnect()
        {
            if (Connection.State != System.Data.ConnectionState.Closed && Connection.State != System.Data.ConnectionState.Broken)
                Connection?.Close();
            Connection?.Dispose();
            Connection = null;

            Log.WriteLine("Disconnected from MySQL Server at %@", LogLevel.Message, Configuration.Hostname);
        }

        protected int RunSql(string query, params MySqlParameter[] arguments)
        {
            if (!IsConnected)
                throw new InvalidOperationException("The instance is currently not connected to any server.");

            query = ReplaceSqlObjectNames(query);

            using (MySqlCommand command = new MySqlCommand(query, Connection, Transaction))
            {
                command.Parameters.AddRange(arguments);

                AddCommandHistory(command);

                return command.ExecuteNonQuery();
            }
        }

        protected MySqlDataReader RunSqlQuery(string query, params MySqlParameter[] arguments)
        {
            if (!IsConnected)
                throw new InvalidOperationException("The instance is currently not connected to any server.");

            query = ReplaceSqlObjectNames(query);

            using (MySqlCommand command = new MySqlCommand(query, Connection, Transaction))
            {
                command.Parameters.AddRange(arguments);

                AddCommandHistory(command);

                return command.ExecuteReader();
            }
        }

        protected T RunSql<T>(string query, params MySqlParameter[] arguments)
        {
            if (!IsConnected)
                throw new InvalidOperationException("The instance is currently not connected to any server.");

            query = ReplaceSqlObjectNames(query);

            using (MySqlCommand command = new MySqlCommand(query, Connection, Transaction))
            {
                command.Parameters.AddRange(arguments);

                AddCommandHistory(command);

                return (T)command.ExecuteScalar();
            }
        }

        public void Begin()
        {
            if (!IsConnected)
                throw new InvalidOperationException("The instance is currently not connected to any server.");

            try
            {
                if (Configuration.LockTables)
                {
                    Log.WriteLine("Locking remote MySQL table %@ with WRITE exclusive access...", LogLevel.Debug, TableName);
                    int result = RunSql("LOCK TABLES @o_tableName WRITE");
                    Log.WriteLine("Lock success with value %@", LogLevel.Debug, result);
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Failed to acquire exlusive lock for table %@. [%@] %@.", LogLevel.Error, TableName, ex.GetType().Name, ex.Message);
                throw;
            }

            try
            {
                if (Configuration.Transactional)
                {
                    Transaction = Connection.BeginTransaction(System.Data.IsolationLevel.Serializable);
                    Log.WriteLine("Transaction initiated on the MySQL Server connection with isolation level: %@.", LogLevel.Debug, Transaction.IsolationLevel.ToString());
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Failed to initiate a new transaction for table %@. [%@] %@.", LogLevel.Error, TableName, ex.GetType().Name, ex.Message);
                throw;
            }
        }

        public void End(bool commit = false)
        {
            if (Configuration.Transactional)
            {
                if (commit)
                {
                    Log.WriteLine("Commiting changes on MySQL Server...", LogLevel.Debug);
                    Transaction?.Commit();
                }
                else
                {
                    Log.WriteLine("Discarding changes on MySQL Server...", LogLevel.Debug);
                    Transaction?.Rollback();
                }

                Transaction?.Dispose();
                Transaction = null;
            }

            if (Configuration.LockTables)
            {
                Log.WriteLine("Unlocking remote MySQL tables...", LogLevel.Debug);
                int result = RunSql("UNLOCK TABLES");
                Log.WriteLine("Done unlocking tables.", LogLevel.Debug, result);
            }
        }

        public void CreateTable()
        {
            int result = RunSql(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceString("NroIpDatabaseGenerator.SQL.Scripts.MySQL.CreateTable.sql"));
        }

        public bool GetTableExists()
        {
            try
            {
                int result = RunSql(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceString("NroIpDatabaseGenerator.SQL.Scripts.MySQL.TableExistsCheck.sql"));
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public void DropTable()
        {
            int result = RunSql(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceString("NroIpDatabaseGenerator.SQL.Scripts.MySQL.DropTable.sql"));
        }

        public void TruncateTable()
        {
            int result = RunSql(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceString("NroIpDatabaseGenerator.SQL.Scripts.MySQL.TruncateTable.sql"));
        }

        public int InsertRecord(RIR.Package.Reader.RirIpRecord record)
        {
            int result = RunSql(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceString("NroIpDatabaseGenerator.SQL.Scripts.MySQL.InsertSingleRecord.sql"),
                new MySqlParameter("ipcidr", record.AddressCidr),
                new MySqlParameter("ipdec", record.AddressDec),
                new MySqlParameter("addressCount", record.AddressCount),
                new MySqlParameter("countryCode", record.CountryIsoCode),
                new MySqlParameter("countryName", record.Country?.Trim()),
                new MySqlParameter("updateDate", record.AssignDate),
                new MySqlParameter("_status", record.Status.ToString().ToUpperInvariant()),
                new MySqlParameter("city", record.City?.Trim())
                );

            return result;
        }

        public int InsertRecords(RIR.Package.Reader.RirIpRecord[] records)
        {
            StringBuilder query = new StringBuilder(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceString("NroIpDatabaseGenerator.SQL.Scripts.MySQL.InsertBulkRecords.Header.sql"));

            int index = 0;
            List<MySqlParameter> parameters = new List<MySqlParameter>(records.Length);
            foreach (RIR.Package.Reader.RirIpRecord record in records)
            {
                query.Append(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceString("NroIpDatabaseGenerator.SQL.Scripts.MySQL.InsertBulkRecords.RecordData.sql").Replace("@_i", index.ToString()));

                parameters.AddRange(new MySqlParameter[] {
                    new MySqlParameter("ipcidr_" + index.ToString(), record.AddressCidr),
                    new MySqlParameter("ipdec_" + index.ToString(), record.AddressDec),
                    new MySqlParameter("addressCount_" + index.ToString(), record.AddressCount),
                    new MySqlParameter("countryCode_" + index.ToString(), record.CountryIsoCode),
                    new MySqlParameter("countryName_" + index.ToString(), record.Country?.Trim()),
                    new MySqlParameter("updateDate_" + index.ToString(), record.AssignDate),
                    new MySqlParameter("_status_" + index.ToString(), record.Status.ToString().ToUpperInvariant()),
                    new MySqlParameter("city_" + index.ToString(), record.City?.Trim())
                });

                if (index < records.Length - 1)
                    query.Append(",\r\n");

                index++;
            }
            query.Append(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceString("NroIpDatabaseGenerator.SQL.Scripts.MySQL.InsertBulkRecords.Tail.sql"));
            int result = RunSql(query.ToString(), parameters.ToArray());

            return result;
        }

        public override void DumpCommandsToStream(Stream stream)
        {
            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                writer.WriteLine("# SQL Commands for MySQL Server");
                writer.WriteLine();

                foreach (string sql in CommandHistory)
                    writer.WriteLine(sql);

                writer.WriteLine();
                writer.WriteLine("# End of SQL file");
            }
        }
    }
}
