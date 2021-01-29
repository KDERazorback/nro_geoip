using com.razorsoftware.SettingsLib.CommandLine;
using com.RazorSoftware.Logging;
using NroIpDatabaseGenerator.RIR;
using NroIpDatabaseGenerator.RIR.Package;
using System;
using System.Collections.Generic;
using System.IO;

namespace NroIpDatabaseGenerator
{
    static class Program
    {
        static int EXIT_SUCCESS = 0;
        static int EXIT_ARGS_INVALID = 1;
        static int EXIT_EULA_NOT_ACCEPTED = 2;
        static int EXIT_INVALID_RESOURCES = 3;
        static int EXIT_PROCESSING_ERROR = 4;

        static int Main(string[] args)
        {
            Log.Initialize(true);
            Log.Console.Enabled = true;
            Log.Console.AutoColorOutput = true;
            Log.WriteLine("NRO Geolocation DB Generator");
            Log.WriteLine("NRO Registry Processing Tool for Geolocation IP Database generation");
            Log.WriteLine("By RazorSoftware.dev (c) 2020");
            Log.WriteLine("This tool is intended to be used for generating IP-to-Country databases from NRO public stats.");
            Log.WriteLine("All other usages for this tool are explicitly prohibited.");
            Log.WriteLine();

            DateTime startTime = DateTime.Now;

            FileInfo EulaFile = new FileInfo("EULA.txt");

            if (!EulaFile.Exists)
            {
                File.WriteAllText(EulaFile.FullName, Resources.EULA);
                Log.WriteColoredLine("A copy of the EULA has been written to: %@", ConsoleColor.Green, LogLevel.Message, EulaFile.FullName);
            }

            if (args == null || args.Length < 1)
            {
                Log.WriteColoredLine("No arguments given.", ConsoleColor.Red, LogLevel.Error);
                Log.WriteLine();

                FileInfo answersFileTemplate = new FileInfo("answers_file.txt");
                if (!answersFileTemplate.Exists)
                {
                    Settings settings = new Settings();
                    using (FileStream fs = new FileStream(answersFileTemplate.FullName, FileMode.Create, FileAccess.Write, FileShare.None))
                        settings.SaveToStream(fs);
                    Log.WriteColoredLine("A template Answers file has been written to the application directory.\nYou can use it to customize the Application behavior.", ConsoleColor.Green, LogLevel.Message);
                    Log.WriteLine();
                }

                Log.WriteLine("You must specify at least an answers file by supplying the argument --answers=<file> or /answers=<file>");
                Log.WriteLine("      Usage: nroipdbgen --answers=<file>");

                return EXIT_ARGS_INVALID;
            }

            Settings.Current = new Settings();
            Processor.TargetInstance = Settings.Current;
            if (!Processor.Parse(args))
            {
                Log.WriteColoredLine("Failed to process command line parameters.", ConsoleColor.Red, LogLevel.Error);
                return EXIT_ARGS_INVALID;
            }

            if (Settings.Current.ShowHelp)
            {
                Log.WriteLine("NOT IMPLEMENTED.");
                return EXIT_ARGS_INVALID;
            }

            if (!string.IsNullOrWhiteSpace(Settings.Current.AnswersFile))
            {
                Log.WriteLine("Loading answers file from %@...", LogLevel.Message, Settings.Current.AnswersFile);

                using (FileStream fs = new FileStream(Settings.Current.AnswersFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    Settings.Current = (Settings)Settings.LoadFromStream(fs, typeof(Settings));

                Log.WriteLine("Answers file loaded.");
            }

            if (Settings.Current.VerboseOutput)
                Log.Console.MinimumLevel = LogLevel.Debug;

            FileInfo Alpha2File = new FileInfo(Settings.Current.Alpha2CodesFile);
            if (!Alpha2File.Exists)
                File.WriteAllText(Alpha2File.FullName, Resources.IsoAlpha2CountryCodes);
            Alpha2File.Refresh();

            try
            {
                Settings.IsoCountryCodes = new Alpha2.IsoAlpha2CountryCodes(Alpha2File);
                Log.WriteLine("Loaded %@ country codes from file %@ for ISO-3166-2.", LogLevel.Debug, Settings.IsoCountryCodes.Count.ToString("N0"), Alpha2File.Name);
            }
            catch (Exception ex)
            {
                Log.WriteLine("Cannot load ISO-3166-2 country file from disk at %@. [%@] %@.", LogLevel.Error, Alpha2File.FullName, ex.GetType().Name, ex.Message);
                return EXIT_INVALID_RESOURCES;
            }

            try
            {
                while (Console.KeyAvailable)
                    Console.ReadKey();
            }
            catch (Exception ex)
            {
                if (!Settings.Current.HeadlessMode)
                {
                    Log.WriteLine("Console input stream not available. Cannot continue unless --headless or /headless is specified.", LogLevel.Error);
                    Log.WriteLine("[%@] %@", LogLevel.Error, ex.GetType().Name, ex.Message);

                    return EXIT_ARGS_INVALID;
                } else
                    Log.WriteLine("Console input stream not available. Continuing in Headless mode as requested.");
            }

            Log.WriteColoredLine("BEGIN OF PARTIAL EULA", ConsoleColor.Cyan);

            Log.WriteLine(Resources.EULA_Redacted);

            Log.WriteColoredLine("END OF PARTIAL EULA", ConsoleColor.Cyan);
            Log.WriteLine("Full EULA location: %@", LogLevel.Message, EulaFile.FullName);

            if (Settings.Current.IsEulaAccepted)
            {
                Log.WriteColoredLine("EULA has been already accepted by user configuration.", ConsoleColor.Green, LogLevel.Message);
            } else
            {
                Log.WriteLine();
                if (Settings.Current.HeadlessMode)
                {
                    Log.WriteLine("Cannot continue. EULA is not accepted and the application is currently running in Headless mode.", LogLevel.Error);
                    Log.WriteLine("Remove the --headless or /headless in command args or accept the EULA on the answers file to continue.", LogLevel.Error);
                    return EXIT_ARGS_INVALID;
                }
                Log.WriteLine("You must accept the EULA before continuing (type \"I Agree\" without quotes): ");
                string acceptEulaString = Console.ReadLine();

                if (!string.Equals(acceptEulaString, "I Agree", StringComparison.OrdinalIgnoreCase))
                {
                    Log.WriteLine("EULA not accepted. Cannot continue.");
                    return EXIT_EULA_NOT_ACCEPTED;
                }

                Log.WriteColoredLine("EULA HAS BEEN ACCEPTED.", ConsoleColor.Cyan);
            }

            if (Settings.Current.IsSettingsFileVanilla)
            {
                Log.WriteLine("Cannot load answers file. Please take the time to read it and customize it to your needs.", LogLevel.Error);
                return EXIT_EULA_NOT_ACCEPTED;
            }

            Log.WriteLine();
            Log.WriteLine();
            Log.WriteLine();
            Log.WriteLine();

            Log.AutoWrappingEnabled = true;


            // Download packages
            List<RirPackage> packages = new List<RirPackage>();

            if (Settings.Current.UseAFRINIC)
            {
                AFRINIC rir = new AFRINIC();
                rir.OfflineMode = Settings.Current.OfflineMode;
                //rir.PublicFtpHostname = "127.0.0.1";
                rir.RequestEmail = Settings.Current.FtpContactEmailAddress;
                RirPackage package = rir.DownloadPackage();

                packages.Add(package);
            }

            if (Settings.Current.UseAPNIC)
            {
                APNIC rir = new APNIC();
                rir.OfflineMode = Settings.Current.OfflineMode;
                //rir.PublicFtpHostname = "127.0.0.1";
                rir.RequestEmail = Settings.Current.FtpContactEmailAddress;
                RirPackage package = rir.DownloadPackage();

                packages.Add(package);
            }

            if (Settings.Current.UseARIN)
            {
                ARIN rir = new ARIN();
                rir.OfflineMode = Settings.Current.OfflineMode;
                //rir.PublicFtpHostname = "127.0.0.1";
                rir.RequestEmail = Settings.Current.FtpContactEmailAddress;
                RirPackage package = rir.DownloadPackage();

                packages.Add(package);
            }

            if (Settings.Current.UseRIPE)
            {
                RIPENCC rir = new RIPENCC();
                rir.OfflineMode = Settings.Current.OfflineMode;
                //rir.PublicFtpHostname = "127.0.0.1";
                rir.RequestEmail = Settings.Current.FtpContactEmailAddress;
                RirPackage package = rir.DownloadPackage();

                packages.Add(package);
            }

            if (Settings.Current.UseLACNIC)
            {
                LACNICv1 rir = new LACNICv1();
                rir.OfflineMode = Settings.Current.OfflineMode;
                //rir.PublicFtpHostname = "127.0.0.1";
                rir.RequestEmail = Settings.Current.FtpContactEmailAddress;
                RirPackage package = rir.DownloadPackage();

                packages.Add(package);
            }


            // Pre-Scan packages
            long totalRecords = 0;
            foreach (RirPackage package in packages)
            {
                package.ScanPackage(Settings.Current.IgnorePackageProcessingErrors);
                if (package.TotalRecordCount > 0)
                    totalRecords += package.TotalRecordCount;
            }

            Log.WriteLine();
            Log.WriteLine("Download succeeded. %@ Total records found for the selected RIRs.", LogLevel.Message, totalRecords.ToString("N0"));

            // Update MySQL
            if (Settings.Current.UpdateMySQL)
            {
                SQL.MySqlDriverConfiguration config = new SQL.MySqlDriverConfiguration(Settings.Current.RemoteMySQLHost,
                    Settings.Current.RemoteMySQLUsername,
                    Settings.Current.RemoteMySQLPassword,
                    Settings.Current.RemoteMySQLSchema,
                    Settings.Current.RemoteMySQLPrefix);
                config.TableName = Settings.Current.RemoteMySQLTableName;

                SQL.MySqlIpDatabase db = new SQL.MySqlIpDatabase(config);
                db.Connect();

                // Check if table exists
                if (db.GetTableExists())
                {
                    Log.WriteLine("Remote MySQL Table %@ is present on the remote server.", LogLevel.Debug, db.TableName);

                    if (Settings.Current.RemoteMySQLTruncateExisting)
                        db.TruncateTable();

                    if (Settings.Current.RemoteMySQLDropExisting)
                        db.DropTable();
                }

                // Check if table is missing
                if (!db.GetTableExists())
                {
                    Log.WriteLine("Remote MySQL Table %@ is NOT present on the remote server.", LogLevel.Debug, db.TableName);

                    if (Settings.Current.RemoteMySQLCreateMissing)
                        db.CreateTable();
                    else
                        throw Log.WriteAndMakeException("The specified Table name doesnt exists on the remote MySQL Server.", LogLevel.Error, typeof(InvalidOperationException));
                }

                db.Begin();

                Log.WriteLine("Updating remote MySQL Table %@::%@.%@:%@...", LogLevel.Message, config.Hostname, config.Schema, config.TablePrefix, config.TableName);

                int processedRecords = 0;
                int confirmedRecords = 0;
                ConsoleProgressBar progress = new ConsoleProgressBar();
                if (Settings.Current.HeadlessMode)
                    progress.Enabled = false;
                else
                {
                    progress.Enabled = true;
                    progress.Maximum = totalRecords;
                    progress.Value = 0;
                }

                Queue<RIR.Package.Reader.RirIpRecord> queuedRecords = new Queue<RIR.Package.Reader.RirIpRecord>(Settings.Current.RemoteMySQLRecordsPerQuery);
                foreach (RirPackage package in packages)
                {
                    foreach (RirPackageEntry entry in package.Entries)
                    {
                        RIR.Package.Reader.RirFileReader reader = entry.Open();
                        RIR.Package.Reader.RirIpRecord record;
                        while (true)
                        {
                            try
                            {
                                record = reader.NextEntry();
                            }
                            catch (Exception ex)
                            {
                                if (!Settings.Current.HeadlessMode && Console.CursorLeft > 0)
                                    Console.WriteLine();
                                if (Settings.Current.IgnorePackageProcessingErrors)
                                {
                                    Log.WriteLine("An error occurred while processing record #%@ from package %@.%@ [%@] %@.", LogLevel.Warning, processedRecords.ToString("N0"), package.RirName, entry.Identifier, ex.GetType().Name, ex.Message);
                                    continue;
                                }

                                Log.WriteLine("An error occurred while processing record #%@ from package %@.%@ [%@] %@.", LogLevel.Error, processedRecords.ToString("N0"), package.RirName, entry.Identifier, ex.GetType().Name, ex.Message);
                                db.Disconnect();
                                return EXIT_PROCESSING_ERROR;
                            }

                            if (record != null)
                                queuedRecords.Enqueue(record);

                            if (queuedRecords.Count >= Settings.Current.RemoteMySQLRecordsPerQuery || record == null)
                            {
                                processedRecords += queuedRecords.Count;
                                confirmedRecords += db.InsertRecords(queuedRecords.ToArray());
                                queuedRecords.Clear();
                                progress.Value = processedRecords;
                            }

                            if (progress.NeedsRedraw)
                                progress.Refresh();

                            if (record == null)
                                break;
                        }
                    }
                }
                if (!Settings.Current.HeadlessMode && Console.CursorLeft > 0)
                    Console.WriteLine();

                Log.WriteLine("%@ out of %@ MySQL Records updated on the remote database.", LogLevel.Message, confirmedRecords.ToString("N0"), processedRecords.ToString("N0"));

                if (confirmedRecords != processedRecords)
                    Log.WriteLine("Confirmed record count (%@) and processed record count (%@) dont match!. Database may be corrupt.", LogLevel.Warning, confirmedRecords.ToString("N0"), processedRecords.ToString("N0"));

                db.End(true);
                db.Disconnect();

                if (Settings.Current.MySQLDumpCommands)
                {
                    FileInfo dumpFile = new FileInfo(Settings.Current.MySQLDumpFilename);

                    if (!dumpFile.Directory.Exists)
                        dumpFile.Directory.Create();
                    Log.WriteLine("Writting SQL history to file %@...", LogLevel.Debug, dumpFile.Name);
                    using (FileStream fs = new FileStream(dumpFile.FullName, FileMode.Create, FileAccess.Write, FileShare.None))
                        db.DumpCommandsToStream(fs);
                }
                Log.WriteLine("MySQL table generation completed.", LogLevel.Debug);
            }

            // Generate SQLite
            if (Settings.Current.GenerateSQLite)
            {
                SQL.SQLiteDriverConfiguration config = new SQL.SQLiteDriverConfiguration(Settings.Current.SQLiteFilename);
                config.TableName = Settings.Current.SQLiteTableName;

                SQL.SQLiteIpDatabase db = new SQL.SQLiteIpDatabase(config);
                db.Connect();

                // Check if table exists
                if (db.GetTableExists())
                {
                    Log.WriteLine("SQLite Table %@ is present on file.", LogLevel.Debug, db.TableName);

                    if (Settings.Current.SQLiteTruncateExisting)
                        db.TruncateTable();

                    if (Settings.Current.SQLiteDropExisting)
                        db.DropTable();
                }

                // Check if table is missing
                if (!db.GetTableExists())
                {
                    Log.WriteLine("SQLite Table %@ is NOT present on file.", LogLevel.Debug, db.TableName);

                    if (Settings.Current.SQLiteCreateMissing)
                        db.CreateTable();
                    else
                        throw Log.WriteAndMakeException("The specified Table name doesnt exists on file.", LogLevel.Error, typeof(InvalidOperationException));
                }

                Log.WriteLine("Updating SQLite Table %@ at file %@...", LogLevel.Message, config.TableName, config.DatabaseFile.Name);

                int processedRecords = 0;
                int confirmedRecords = 0;
                ConsoleProgressBar progress = new ConsoleProgressBar();
                if (Settings.Current.HeadlessMode)
                    progress.Enabled = false;
                else
                {
                    progress.Enabled = true;
                    progress.Maximum = totalRecords;
                    progress.Value = 0;
                }

                Queue<RIR.Package.Reader.RirIpRecord> queuedRecords = new Queue<RIR.Package.Reader.RirIpRecord>(Settings.Current.SQLiteRecordsPerQuery);
                foreach (RirPackage package in packages)
                {
                    foreach (RirPackageEntry entry in package.Entries)
                    {
                        RIR.Package.Reader.RirFileReader reader = entry.Open();
                        RIR.Package.Reader.RirIpRecord record;
                        while (true)
                        {
                            try
                            {
                                record = reader.NextEntry();
                            }
                            catch (Exception ex)
                            {
                                if (!Settings.Current.HeadlessMode && Console.CursorLeft > 0)
                                    Console.WriteLine();
                                if (Settings.Current.IgnorePackageProcessingErrors)
                                {
                                    Log.WriteLine("An error occurred while processing record #%@ from package %@.%@ [%@] %@.", LogLevel.Warning, processedRecords.ToString("N0"), package.RirName, entry.Identifier, ex.GetType().Name, ex.Message);
                                    continue;
                                }

                                Log.WriteLine("An error occurred while processing record #%@ from package %@.%@ [%@] %@.", LogLevel.Error, processedRecords.ToString("N0"), package.RirName, entry.Identifier, ex.GetType().Name, ex.Message);
                                db.Disconnect();
                                return EXIT_PROCESSING_ERROR;
                            }

                            if (record != null)
                                queuedRecords.Enqueue(record);

                            if (queuedRecords.Count >= Settings.Current.SQLiteRecordsPerQuery || record == null)
                            {
                                processedRecords += queuedRecords.Count;
                                confirmedRecords += db.InsertRecords(queuedRecords.ToArray());
                                queuedRecords.Clear();
                                progress.Value = processedRecords;
                            }

                            if (progress.NeedsRedraw)
                                progress.Refresh();

                            if (record == null)
                                break;
                        }
                    }
                }
                if (!Settings.Current.HeadlessMode && Console.CursorLeft > 0)
                    Console.WriteLine();

                Log.WriteLine("%@ out of %@ SQLite Records updated on file.", LogLevel.Message, confirmedRecords.ToString("N0"), processedRecords.ToString("N0"));

                if (confirmedRecords != processedRecords)
                    Log.WriteLine("Confirmed record count (%@) and processed record count (%@) dont match!. Database may be corrupt.", LogLevel.Warning, confirmedRecords.ToString("N0"), processedRecords.ToString("N0"));

                db.Disconnect();

                if (Settings.Current.SQLiteDumpCommands)
                {
                    FileInfo dumpFile = new FileInfo(Settings.Current.SQLiteDumpFilename);

                    if (!dumpFile.Directory.Exists)
                        dumpFile.Directory.Create();
                    Log.WriteLine("Writting SQL history to file %@...", LogLevel.Debug, dumpFile.Name);
                    using (FileStream fs = new FileStream(dumpFile.FullName, FileMode.Create, FileAccess.Write, FileShare.None))
                        db.DumpCommandsToStream(fs);
                }
                Log.WriteLine("SQLite table generation completed.", LogLevel.Debug);
            }

            TimeSpan runTime = DateTime.Now - startTime;
            Log.WriteLine("Operation completed in %@. Quitting...", LogLevel.Message, runTime.ToString("d':'hh':'mm':'ss"));
            return EXIT_SUCCESS;
        }
    }
}
