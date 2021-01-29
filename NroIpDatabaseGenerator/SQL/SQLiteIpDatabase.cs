using com.RazorSoftware.Logging;
using NroIpDatabaseGenerator.Extensions;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text;

namespace NroIpDatabaseGenerator.SQL
{
    internal class SQLiteIpDatabase : IpDatabase
    {
        public SQLiteDriverConfiguration Configuration { get; }
        protected new SQLiteConnection Connection { get; set; }
        public override string TableName => Configuration.TablePrefix + Configuration.TableName;
        public bool IsConnected
        {
            get
            {
                return (Connection != null && Connection.State == System.Data.ConnectionState.Open);
            }
        }


        public SQLiteIpDatabase(SQLiteDriverConfiguration configuration) : base()
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));


            SQLiteConnectionStringBuilder builder = new SQLiteConnectionStringBuilder()
            {
                DataSource = configuration.DatabaseFile.FullName,
                Password = "**redacted**",
            };

            Log.WriteLine("Connecting to SQLite instance with connection string: %@...", LogLevel.Debug, builder.ConnectionString);
            builder.Password = configuration.Password;
            Connection = new SQLiteConnection(builder.ConnectionString);
        }

        public override void Connect()
        {
            if (IsConnected)
                throw new InvalidOperationException("There is already a connection active for this instance.");

            try
            {
                Log.Write("Opening SQLite file %@... ", LogLevel.Message, Configuration.DatabaseFile.Name);

                Connection.Open();

                Log.WriteColoredLine("OK ! SQLite %@", ConsoleColor.Green, LogLevel.Message, Connection.ServerVersion);
            }
            catch (Exception ex)
            {
                Log.WriteColoredLine("FAIL !", ConsoleColor.Red);
                Log.WriteLine("Failed to open a connection to the target file. [%@] %@.", LogLevel.Error, ex.GetType().Name, ex.Message);
                throw;
            }
        }

        public override void Disconnect()
        {
            if (Connection.State != System.Data.ConnectionState.Closed && Connection.State != System.Data.ConnectionState.Broken)
                Connection?.Close();
            Connection?.Dispose();
            Connection = null;

            Log.WriteLine("Disconnected from SQLite file at %@", LogLevel.Message, Configuration.DatabaseFile.Name);
        }

        protected int RunSql(string query, params SQLiteParameter[] arguments)
        {
            if (!IsConnected)
                throw new InvalidOperationException("The instance is currently not connected to any file.");

            query = ReplaceSqlObjectNames(query);

            using (SQLiteCommand command = new SQLiteCommand(query, Connection))
            {
                command.Parameters.AddRange(arguments);

                AddCommandHistory(command);

                return command.ExecuteNonQuery();
            }
        }

        protected SQLiteDataReader RunSqlQuery(string query, params SQLiteParameter[] arguments)
        {
            if (!IsConnected)
                throw new InvalidOperationException("The instance is currently not connected to any file.");

            query = ReplaceSqlObjectNames(query);

            using (SQLiteCommand command = new SQLiteCommand(query, Connection))
            {
                command.Parameters.AddRange(arguments);

                AddCommandHistory(command);

                return command.ExecuteReader();
            }
        }

        protected T RunSql<T>(string query, params SQLiteParameter[] arguments)
        {
            if (!IsConnected)
                throw new InvalidOperationException("The instance is currently not connected to any file.");

            query = ReplaceSqlObjectNames(query);

            using (SQLiteCommand command = new SQLiteCommand(query, Connection))
            {
                command.Parameters.AddRange(arguments);

                AddCommandHistory(command);

                return (T)command.ExecuteScalar();
            }
        }

        public void CreateTable()
        {
            int result = RunSql(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceString("NroIpDatabaseGenerator.SQL.Scripts.SQLite.CreateTable.sql"));
        }

        public bool GetTableExists()
        {
            try
            {
                int result = RunSql(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceString("NroIpDatabaseGenerator.SQL.Scripts.SQLite.TableExistsCheck.sql"));
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public void DropTable()
        {
            int result = RunSql(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceString("NroIpDatabaseGenerator.SQL.Scripts.SQLite.DropTable.sql"));
        }

        public void TruncateTable()
        {
            int result = RunSql(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceString("NroIpDatabaseGenerator.SQL.Scripts.SQLite.TruncateTable.sql"));
        }

        public int InsertRecord(RIR.Package.Reader.RirIpRecord record)
        {
            int result = RunSql(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceString("NroIpDatabaseGenerator.SQL.Scripts.SQLite.InsertSingleRecord.sql"),
                new SQLiteParameter("ipcidr", record.AddressCidr),
                new SQLiteParameter("ipdec", record.AddressDec),
                new SQLiteParameter("addressCount", record.AddressCount),
                new SQLiteParameter("countryCode", record.CountryIsoCode),
                new SQLiteParameter("countryName", record.Country?.Trim()),
                new SQLiteParameter("updateDate", record.AssignDate),
                new SQLiteParameter("_status", record.Status.ToString().ToUpperInvariant()),
                new SQLiteParameter("city", record.City?.Trim())
                );

            return result;
        }

        public int InsertRecords(RIR.Package.Reader.RirIpRecord[] records)
        {
            StringBuilder query = new StringBuilder(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceString("NroIpDatabaseGenerator.SQL.Scripts.SQLite.InsertBulkRecords.Header.sql"));

            int index = 0;
            List<SQLiteParameter> parameters = new List<SQLiteParameter>(records.Length);
            foreach (RIR.Package.Reader.RirIpRecord record in records)
            {
                query.Append(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceString("NroIpDatabaseGenerator.SQL.Scripts.SQLite.InsertBulkRecords.RecordData.sql").Replace("@_i", index.ToString()));

                parameters.AddRange(new SQLiteParameter[] {
                    new SQLiteParameter("ipcidr_" + index.ToString(), record.AddressCidr),
                    new SQLiteParameter("ipdec_" + index.ToString(), record.AddressDec),
                    new SQLiteParameter("addressCount_" + index.ToString(), record.AddressCount),
                    new SQLiteParameter("countryCode_" + index.ToString(), record.CountryIsoCode),
                    new SQLiteParameter("countryName_" + index.ToString(), record.Country?.Trim()),
                    new SQLiteParameter("updateDate_" + index.ToString(), record.AssignDate),
                    new SQLiteParameter("_status_" + index.ToString(), record.Status.ToString().ToUpperInvariant()),
                    new SQLiteParameter("city_" + index.ToString(), record.City?.Trim())
                });

                if (index < records.Length - 1)
                    query.Append(",\r\n");

                index++;
            }
            query.Append(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceString("NroIpDatabaseGenerator.SQL.Scripts.SQLite.InsertBulkRecords.Tail.sql"));
            int result = RunSql(query.ToString(), parameters.ToArray());

            return result;
        }

        public override void DumpCommandsToStream(Stream stream)
        {
            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                writer.WriteLine("-- SQL Commands for SQLite");
                writer.WriteLine();

                foreach (string sql in CommandHistory)
                    writer.WriteLine(sql);

                writer.WriteLine();
                writer.WriteLine("-- End of SQL file");
            }
        }
    }
}
