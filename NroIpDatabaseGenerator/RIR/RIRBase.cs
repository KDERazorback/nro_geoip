using com.RazorSoftware.Logging;
using NroIpDatabaseGenerator.RIR.Package;
using NroIpDatabaseGenerator.RIR.Package.Reader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NroIpDatabaseGenerator.RIR
{
    internal abstract class RIRBase : IDisposable
    {
        protected long FtpCurrentTransferSize = 0;
        protected double LastFtpProgressPercent = -2;
        protected long LastFtpProgressValue = 0;
        public string CacheDirectory { get; set; } = "cache";
        public virtual FluentFTP.FtpClient FtpClient { get; set; }
        public abstract string Identifier { get; }
        public abstract string PublicFtpChecksumsUrl { get; }
        public abstract string PublicFtpHostname { get; set; }
        public virtual int PublicFtpPort { get; set;  } = 21;
        public abstract string PublicFtpStatsUrl { get; }
        public string RequestEmail { get; set; } = null;
        public abstract string SafeFileIdentifier { get; }
        public bool OfflineMode { get; set; } = false;
        public virtual void DisconnectServer()
        {
            if (FtpClient == null)
                return;

            if (FtpClient.IsConnected)
                FtpClient.Disconnect();

            FtpClient = null;
            Log.WriteLine("Disconnected from FTP server.", LogLevel.Debug);
        }

        public void Dispose()
        {
            DisconnectServer();
        }

        /// <summary>
        /// Downloads the required data from the remote FTP Server, and returns a package composed of the already-decompressed and verified PackageEntries
        /// </summary>
        /// <returns></returns>
        public abstract RirPackage DownloadPackage();

        public virtual FileInfo DownloadFile(string url, string filename)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url));

            if (string.IsNullOrWhiteSpace(RequestEmail) || !RequestEmail.Contains('@') || RequestEmail.Length < 10)
            {
                Log.WriteLine("Internal error. Cannot request FTP content because no contact email is provided.", LogLevel.Error);
                throw new InvalidOperationException("No valid requesting contact email provided. Please set the RequestEmail to continue. Cant comply.");
            }

            FileInfo localFile = new FileInfo(Path.Combine(CacheDirectory, filename));

            if (!localFile.Directory.Exists)
                localFile.Directory.Create();

            if (OfflineMode)
            {
                localFile.Refresh();
                if (!localFile.Exists)
                    throw new FileNotFoundException("The downloaded file cannot be located. Application is in offline mode.");

                Log.WriteLine("Offline mode. No file download for %@. Cached file size: %@ bytes", LogLevel.Message, localFile.Name, localFile.Length.ToString("N0"));

                return localFile;
            }

            if (FtpClient == null)
            {
                FtpClient = new FluentFTP.FtpClient(PublicFtpHostname, PublicFtpPort, new System.Net.NetworkCredential("anonymous", RequestEmail));
                FtpClient.ConnectTimeout = 7000;
                FtpClient.DataConnectionConnectTimeout = 7000;
                FtpClient.DataConnectionReadTimeout = 10000;
                FtpClient.ReadTimeout = 7000;
            }
            else
                Log.WriteLine("Reusing existing FTP connection to ftp://%@@%@:%@", LogLevel.Debug, FtpClient.Credentials.UserName, PublicFtpHostname, PublicFtpPort.ToString());

            int tryIndex = 0;
            int maxTries = 10;
            int tryDelayFormulaBase = 2;
            bool abortLoop = false;
            while (!FtpClient.IsConnected)
            {
                try
                {
                    if (tryIndex >= maxTries)
                    {
                        abortLoop = true;
                        throw new IOException("Cannot connect to the remote server.");
                    }

                    int delay = tryIndex > 0 ? (int)Math.Pow(tryDelayFormulaBase, tryIndex) : 0;
                    if (delay > 0)
                    {
                        if (Settings.Current.HeadlessMode)
                        {
                            Log.Write("Retrying in %@ seconds... ", LogLevel.Message, delay.ToString("N0"));
                            System.Threading.Thread.Sleep(delay * 1000);
                        }
                        else
                        {
                            Log.Write("Retrying in %@ seconds. Press Q at any time to abort. ", LogLevel.Message, delay.ToString("N0"));
                            for (int i = 0; i < delay; i++)
                            {
                                System.Threading.Thread.Sleep(1000);
                                while (Console.KeyAvailable)
                                {
                                    if (Console.ReadKey().Key == ConsoleKey.Q)
                                    {
                                        abortLoop = true;
                                        throw new TimeoutException("The operation timed out and a retry attempt was aborted");
                                    }
                                }
                            }
                        }
                        Log.WriteLine("Retrying...");
                    }

                    if (tryIndex > 0)
                        Log.Write("Try %@ of %@. ", LogLevel.Message, tryIndex + 1, maxTries);
                    tryIndex++;

                    Log.Write("Connecting to the remote FTP host (%@) at ftp://%@@%@:%@... ", LogLevel.Message, Identifier, FtpClient.Credentials.UserName, PublicFtpHostname, PublicFtpPort.ToString());
                    FtpClient.RetryAttempts = 1;
                    FluentFTP.FtpProfile profile = FtpClient.AutoConnect();

                    if (profile == null || !FtpClient.IsConnected)
                        throw new TimeoutException("The operation timed out.");

                    Log.WriteColoredLine("OK! (%@)", ConsoleColor.Green, LogLevel.Message, profile.DataConnection.ToString());
                }
                catch (Exception ex)
                {
                    Log.WriteColoredLine("FAIL!", ConsoleColor.Red, LogLevel.Error);
                    Log.WriteLine("Failed to connect. %@. %@", LogLevel.Error, ex.GetType().Name, ex.Message);

                    if (abortLoop)
                        throw;
                }
            }

            Log.WriteLine("Connected to ftp://%@@%@:%@/. Server Type: %@", LogLevel.Debug, FtpClient.Credentials.UserName, PublicFtpHostname, PublicFtpPort.ToString(), FtpClient.SystemType);
            Log.WriteLine("Requesting file info for %@...", LogLevel.Debug, url);

            try
            {
                FluentFTP.FtpListItem remoteFileInfo = FtpClient.GetObjectInfo(url);
                if (remoteFileInfo == null)
                    throw new IOException("The requested FTP file does not exists.");

                FtpCurrentTransferSize = FtpClient.GetFileSize(url);
                Log.WriteLine("   File size: %@ bytes.", LogLevel.Debug, FtpCurrentTransferSize.ToString("N0"));
                if (FtpCurrentTransferSize == 0)
                {
                    //FtpClient.Disconnect();
                    throw new IOException("The requested file is empty.");
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("An error occurred while requesting file info. %@. %@", LogLevel.Warning, ex.GetType().Name, ex.Message);
                throw;
            }

            try
            {
                using (FileStream fs = new FileStream(localFile.FullName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    FtpClient.Download(fs, url, progress: FtpTransferProgressUpdated);

                    if (Console.CursorLeft > 0)
                        Log.WriteLine();

                    if (fs.Position == FtpCurrentTransferSize || FtpCurrentTransferSize == -1)
                        Log.WriteLine("File transfer completed. %@ bytes received.", LogLevel.Debug, fs.Position.ToString("N0"));
                    else
                    {
                        Log.WriteLine("File transfer completed but file size differs!!. Expected %@ but received %@ bytes.", LogLevel.Error, FtpCurrentTransferSize.ToString("N0"), fs.Position.ToString("N0"));

                        if (!Settings.Current.IgnoreFtpTransferSizeMismatch)
                        {
                            //FtpClient.Disconnect();
                            throw new IOException("The received file doesnt match the expected file length.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("An error occurred while requesting the file. %@. %@. Aborted.", LogLevel.Error, ex.GetType().Name, ex.Message);
                throw;
            }

            //FtpClient.Disconnect();
            //Log.WriteLine("Disconnected from FTP server.", LogLevel.Debug);

            localFile.Refresh();
            return localFile;
        }

        public virtual FileInfo[] DecompressArchive(string rootDir, FileInfo archive, RirCompressionType type)
        {
            if (archive == null)
                throw new ArgumentNullException(nameof(archive));

            if (!archive.Exists || archive.Length < 1)
                throw new IOException("The archive is either invalid or missing.");

            DirectoryInfo targetDir = new DirectoryInfo(Path.Combine(CacheDirectory, rootDir));

            if (targetDir.Exists)
                targetDir.Delete(true);
            targetDir.Refresh();
            if (!targetDir.Exists)
                targetDir.Create();
            targetDir.Refresh();

            if (Settings.Current.VerboseOutput)
                Log.WriteLine("Decompressing archive %@ as type %@...", LogLevel.Debug, archive.Name, type.ToString());

            List<FileInfo> outputFiles = new List<FileInfo>();

            using (FileStream archiveStream = new FileStream(archive.FullName, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                Stream decompressor = null;
                System.IO.Compression.ZipArchive zipArchive = null;
                switch (type)
                {
                    case RirCompressionType.None:
                        decompressor = archiveStream;
                        break;
                    case RirCompressionType.Gzip:
                        decompressor = new System.IO.Compression.GZipStream(archiveStream, System.IO.Compression.CompressionMode.Decompress, true);
                        break;
                    case RirCompressionType.Zip:
                        zipArchive = new System.IO.Compression.ZipArchive(archiveStream, System.IO.Compression.ZipArchiveMode.Read, true);
                        break;
                    case RirCompressionType.Deflate:
                        decompressor = new System.IO.Compression.DeflateStream(archiveStream, System.IO.Compression.CompressionMode.Decompress, true);
                        break;
                    default:
                        throw new InvalidOperationException("Internal Error. Unknown compression algorithm.");
                }

                if (zipArchive != null)
                {
                    // Specific case for Zip archives
                    Log.WriteLine("Found %@ entries inside the Zip archive. Decompressing...", LogLevel.Debug, zipArchive.Entries.Count.ToString("N0"));
                    foreach (System.IO.Compression.ZipArchiveEntry entry in zipArchive.Entries)
                    {
                        Log.WriteLine("Decompressing file %@  %@ bytes...", LogLevel.Debug, entry.Name, entry.Length.ToString("N0"));
                        FileInfo targetFile = new FileInfo(Path.Combine(targetDir.FullName, entry.FullName));

                        if (!targetFile.Directory.Exists)
                            targetFile.Directory.Create();

                        using (Stream decompressedStream = targetFile.Open(FileMode.Open))
                        using (FileStream fsout = new FileStream(targetFile.FullName, FileMode.Create, FileAccess.Write, FileShare.None))
                            decompressedStream.CopyTo(fsout);

                        targetFile.Refresh();
                        outputFiles.Add(targetFile);
                    }

                    Log.WriteLine("Archive decompression completed.", LogLevel.Debug);
                }
                else
                {
                    // Common case for single-file compressed streams
                    FileInfo targetFile = new FileInfo(Path.Combine(targetDir.FullName, Path.GetFileNameWithoutExtension(archive.Name)));

                    if (!targetFile.Directory.Exists)
                        targetFile.Directory.Create();

                    using (FileStream fsout = new FileStream(targetFile.FullName, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        Log.WriteLine("Decompressing file %@...", LogLevel.Debug, targetFile.Name);
                        decompressor.CopyTo(fsout);
                        Log.WriteLine("Done. %@ bytes written.", LogLevel.Debug, fsout.Position.ToString("N0"));
                    }

                    targetFile.Refresh();
                    outputFiles.Add(targetFile);
                }
            }

            return outputFiles.ToArray();
        }

        public virtual RirFileReader OpenDatabase(RirPackage package)
        {
            throw new NotImplementedException();
        }
        protected virtual void FtpTransferProgressUpdated(FluentFTP.FtpProgress progress)
        {
            if (!Settings.Current.HeadlessMode)
            {
                StringBuilder text = new StringBuilder(string.Format("{0:N0} bytes transferred. {1:N2}% completed. {2}. ETA: {3}",
                    progress.TransferredBytes,
                    progress.Progress,
                    progress.TransferSpeedToString(),
                    progress.ETA.ToString("d':'hh':'mm':'ss")));

                if (text.Length < Console.BufferWidth - 1)
                    text.Append(' ', Console.BufferWidth - 1 - text.Length);
                if (text.Length > Console.BufferWidth - 1)
                {
                    text.Length = Console.BufferWidth - 1 - 3;
                    text.Append("...");
                }

                if (progress.Progress > LastFtpProgressPercent ||
                    (FtpCurrentTransferSize > 0 &&
                    progress.Progress < 0 &&
                    progress.TransferredBytes >= Math.Min(FtpCurrentTransferSize, LastFtpProgressValue + (1 * 1024 * 1024))))
                {
                    LastFtpProgressPercent = progress.Progress;
                    LastFtpProgressValue = progress.TransferredBytes;

                    Console.CursorLeft = 0;
                    Log.Write(text.ToString());
                }
            }
        }
    }
}