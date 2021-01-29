using com.razorsoftware.SettingsLib;
using com.razorsoftware.SettingsLib.CommandLine;

namespace NroIpDatabaseGenerator
{
    public class Settings : Document
    {
        [TaggedProperty("IsoFile")]
        [DocumentProperty("iso3166-alpha2-filename")]
        [HelpString("Specifies the ISO-3166-2 file to use for 2-digit Country Code to Country Name resolution")]
        public string Alpha2CodesFile { get; set; } = "iso-alpha2.txt";

        [TaggedProperty("Answers")]
        [HelpString("Specifies the Answers file that the Application should use")]
        public string AnswersFile { get; set; } = null;

        [DocumentProperty("contact_email")]
        [TaggedProperty("ContactEmail")]
        [HelpString("Specifies the Contact Email address to use during communication with each RIR")]
        public string FtpContactEmailAddress { get; set; } = null;

        [TaggedProperty("SQLite", "noSQLite")]
        [DocumentProperty("gen_sqlite")]
        [HelpString("Specifies that the Application should produce or update an SQLite file with the new downloaded IP data")]
        public bool GenerateSQLite { get; set; } = false;

        [TaggedProperty("Headless", "Interactive")]
        [HelpString("Specifies that the Application should run without using the STDIN input stream. No prompts will be displayed at all.")]
        public bool HeadlessMode { get; set; } = false;

        [DocumentProperty("ignore_ftp_size_mismatch")]
        [TaggedProperty("RelaxFTP", "StrictFTP")]
        [HelpString("Indicates the application behavior when a downloaded RIR file dont have the expected file size")]
        public bool IgnoreFtpTransferSizeMismatch { get; set; } = false;

        [DocumentProperty("ignore_package_errors")]
        [TaggedProperty("SkipErrors", "ThrowErrors")]
        [HelpString("Skip processing errors while parsing RIR Stats files")]
        public bool IgnorePackageProcessingErrors { get; set; } = false;

        [TaggedProperty("AcceptEULA", "ViewEULA")]
        [DocumentProperty("accept_eula")]
        [HelpString("Indicates that You (the user of this Application) explicitly agrees to the Application EULA.")]
        public bool IsEulaAccepted { get; set; } = false;

        [DocumentProperty("i_didnt_read__set_this_to_false", Default = "false")]
        public bool IsSettingsFileVanilla { get; set; } = true;

        [DocumentProperty("mysql_createifmissing")]
        [TaggedProperty("mysqlCreateMissing", "mysqlFailMissing")]
        [HelpString("Creates a new table for the database on the remote MySQL Host if it doesnt aleady exists. Note that this action is Transaction-less.")]
        public bool RemoteMySQLCreateMissing { get; set; } = false;

        [DocumentProperty("mysql_dropexisting")]
        [TaggedProperty("mysqlLDropExisting", "mysqlKeepExisting")]
        [HelpString("Drops the existing table for the database on the remote MySQL Host if found. Note that this action is Transaction-less.")]
        public bool RemoteMySQLDropExisting { get; set; } = false;

        [DocumentProperty("mysql_host")]
        public string RemoteMySQLHost { get; set; } = "localhost";

        [DocumentProperty("mysql_pass")]
        public string RemoteMySQLPassword { get; set; } = null;

        [DocumentProperty("mysql_port")]
        public int RemoteMySQLPort { get; set; } = 3306;

        [DocumentProperty("mysql_prefix")]
        public string RemoteMySQLPrefix { get; set; } = null;

        [DocumentProperty("mysql_recordsperquery")]
        [HelpString("Specifies the amount of Records that will be sent in a single SQL Query to the remote MySQL Server")]
        public int RemoteMySQLRecordsPerQuery { get; set; } = 400;

        [DocumentProperty("mysql_schema")]
        public string RemoteMySQLSchema { get; set; } = "geoip";

        [DocumentProperty("mysql_tablename")]
        [TaggedProperty("MySQLTable")]
        [HelpString("Specifies the Table name to update on the remote MySQL host")]
        public string RemoteMySQLTableName { get; set; } = "iptable";

        [DocumentProperty("mysql_truncateexisting")]
        [TaggedProperty("mysqlTruncateExisting", "mysqlnoTruncateExisting")]
        [HelpString("Truncates the existing table for the database on the remote MySQL Host if found. Note that this action is Transaction-less.")]
        public bool RemoteMySQLTruncateExisting { get; set; } = false;

        [DocumentProperty("mysql_user")]
        public string RemoteMySQLUsername { get; set; } = null;

        [TaggedProperty("Help", "noHelp")]
        [HelpString("Displays the Application help to the Console and then exits")]
        public bool ShowHelp { get; set; } = false;

        [DocumentProperty("sqlite_createifmissing")]
        [TaggedProperty("sqliteCreateMissing", "sqliteFailMissing")]
        [HelpString("Creates a new table for the database on the SQLite file if it doesnt aleady exists.")]
        public bool SQLiteCreateMissing { get; set; } = false;

        [DocumentProperty("sqlite_dropexisting")]
        [TaggedProperty("sqliteDropExisting", "sqliteKeepExisting")]
        [HelpString("Drops the existing table for the database on the SQLite file if found.")]
        public bool SQLiteDropExisting { get; set; } = false;

        [DocumentProperty("sqlite_file")]
        public string SQLiteFilename { get; set; } = "geoip.sqlite";

        [DocumentProperty("sqlite_recordsperquery")]
        [HelpString("Specifies the amount of Records that will be sent in a single SQL Query to the SQLite Engine")]
        public int SQLiteRecordsPerQuery { get; set; } = 500;

        [DocumentProperty("sqlite_tablename")]
        public string SQLiteTableName { get; set; } = "iptable";

        [DocumentProperty("sqlite_truncateexisting")]
        [TaggedProperty("sqliteTruncateExisting", "sqlitenoTruncateExisting")]
        [HelpString("Truncates the existing table for the database on the SQLite file if found.")]
        public bool SQLiteTruncateExisting { get; set; } = false;

        [TaggedProperty("MySQL", "noMySQL")]
        [DocumentProperty("update_mysql")]
        [HelpString("Specifies that the Application should update a remote MySQL Database with the new downloaded IP data")]
        public bool UpdateMySQL { get; set; } = false;

        [TaggedProperty("AFRINIC", "noAFRINIC")]
        [DocumentProperty("use_afrinic")]
        [HelpString("Allow Application to connect and download data from AFRINIC")]
        public bool UseAFRINIC { get; set; } = true;

        [TaggedProperty("APNIC", "noAPNIC")]
        [DocumentProperty("use_apnic")]
        [HelpString("Allow Application to connect and download data from APNIC")]
        public bool UseAPNIC { get; set; } = true;

        [TaggedProperty("ARIN", "noARIN")]
        [DocumentProperty("use_arin")]
        [HelpString("Allow Application to connect and download data from ARIN")]
        public bool UseARIN { get; set; } = true;

        [TaggedProperty("LACNIC", "noLACNIC")]
        [DocumentProperty("use_lacnic")]
        [HelpString("Allow Application to connect and download data from LACNIC")]
        public bool UseLACNIC { get; set; } = true;

        [TaggedProperty("RIPE", "noRIPE")]
        [DocumentProperty("use_ripe")]
        [HelpString("Allow Application to connect and download data from RIPE NCC")]
        public bool UseRIPE { get; set; } = true;

        [DocumentProperty("verbose")]
        [TaggedProperty("Verbose", "noVerbose")]
        [HelpString("Produces more logging output to the Console")]
        public bool VerboseOutput { get; set; } = false;

        [DocumentProperty("sqlite_dumpcommands")]
        [HelpString("Dumps the SQL commands sent to SQLite to a file on disk.")]
        public bool SQLiteDumpCommands { get; set; } = false;

        [DocumentProperty("sqlite_dumpfile")]
        public string SQLiteDumpFilename { get; set; } = "geoip_sqlite.sql";

        [DocumentProperty("mysql_dumpcommands")]
        [HelpString("Dumps the SQL commands sent to a remote MySQL server to a file on disk.")]
        public bool MySQLDumpCommands { get; set; } = false;

        [DocumentProperty("mysql_dumpfile")]
        public string MySQLDumpFilename { get; set; } = "geoip_mysql.sql";

        [DocumentProperty("offline_mode")]
        [TaggedProperty("offline", "online")]
        [HelpString("Sets the application to Offline mode. It will only use cached files and no request will be sent to any RIR.")]
        public bool OfflineMode { get; set; } = false;

        internal static Settings Current { get; set; }
        internal static Alpha2.IsoAlpha2CountryCodes IsoCountryCodes { get; set; }
    }
}