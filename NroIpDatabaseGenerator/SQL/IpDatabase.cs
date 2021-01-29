using com.RazorSoftware.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Text;

namespace NroIpDatabaseGenerator.SQL
{
    internal abstract class IpDatabase
    {
        protected IpDatabase()
        {
            CommandHistory = new List<string>(4096);
        }
        protected DbConnection Connection { get; set; }
        protected List<string> CommandHistory { get; set; }
        public abstract string TableName { get; }

        public abstract void Connect();
        public abstract void Disconnect();

        protected virtual string ReplaceSqlObjectNames(string query)
        {
            query = query.Replace("@o_tableName", string.Format("`{0}`", TableName));

            return query;
        }

        protected virtual void AddCommandHistory(DbCommand command)
        {
            string commandText = command.CommandText;

            foreach (DbParameter p in command.Parameters)
            {
                commandText = commandText.Replace('@' + p.ParameterName, string.Format("'{0}'", p.Value?.ToString() ?? ""));
            }

#if DEBUG
            System.Diagnostics.Debug.Print("SQL Command Len: " + commandText.Length.ToString("N0"));
#endif

            CommandHistory.Add(commandText);
        }

        public abstract void DumpCommandsToStream(Stream stream);
    }
}
