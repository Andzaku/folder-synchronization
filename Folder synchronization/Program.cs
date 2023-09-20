using System;
using System.IO;
using CommandLine;   // CommandLineParser library
using Serilog;       // Library for logging (in both doc and console)
using Serilog.Events;
using System.Threading;

class Program
{

    //  defining and representing the command-line arguments
    public class Options
    {
        [Option('s', "source", Required = true, HelpText = "Source folder path")]
        public string SourceFolderPath { get; set; }

        [Option('d', "destination", Required = true, HelpText = "Destination folder path")]
        public string DestinationFolderPath { get; set; }

        [Option('i', "interval", Required = true, HelpText = "Synchronization interval in milliseconds")]
        public int SyncIntervalMilliseconds { get; set; }

        [Option('l', "log", Required = true, HelpText = "Log file path")]
        public string LogFilePath { get; set; }
    }

    private static Timer syncTimer;

    static void Main(string[] args)
    {
        // Creating and setting-up logger

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()          
            .WriteTo.Console()
            .WriteTo.File("C:\\Users\\andre\\source\\repos\\Folder synchronization\\log\\log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Command-line argument parsing
        
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed(options =>
            {
                // Initialize and start timer
                syncTimer = new Timer(SynchronizeFolders, options, 0, options.SyncIntervalMilliseconds);

                Log.Information($"Synchronization started. Source: {options.SourceFolderPath}, Destination: {options.DestinationFolderPath}");
                Log.Information($"Logging to: {options.LogFilePath}");
            })
            .WithNotParsed(errors =>
            {
                // Handle command-line parsing errors
                foreach (var error in errors)
                {
                    Log.Error(error.ToString());
                }
            });

        Console.WriteLine("Press Enter to exit.");
        Console.ReadLine();
    }

    private static void SynchronizeFolders(object state)
    {
        if (state is Options options)
        {
            string sourceFolderPath = options.SourceFolderPath;
            string destinationFolderPath = options.DestinationFolderPath;
            string logFilePath = options.LogFilePath;

            // Get a list of all items in the source and destination folders

            var sourceItems = Directory.GetFileSystemEntries(sourceFolderPath, "*", SearchOption.AllDirectories);
            var destinationItems = Directory.GetFileSystemEntries(destinationFolderPath, "*", SearchOption.AllDirectories);

            // Copy new or updated items from source to destination

            foreach (var sourceItem in sourceItems)
            {
                var relativePath = sourceItem.Substring(sourceFolderPath.Length + 1);
                var destinationItem = Path.Combine(destinationFolderPath, relativePath);

                if (File.Exists(sourceItem))
                {
                    // If it's a file, compare and copy if needed

                    if (!File.Exists(destinationItem) || !FileCompare(sourceItem, destinationItem))
                    {
                        string destinationDirectory = Path.GetDirectoryName(destinationItem);

                        if (!Directory.Exists(destinationDirectory))
                        {
                            Directory.CreateDirectory(destinationDirectory);
                        }

                        File.Copy(sourceItem, destinationItem, true);
                        Log.Information($"Copied: {sourceItem} to {destinationItem}");
                    }
                }
                else if (Directory.Exists(sourceItem))
                {
                    // If it's a directory, ensure it exists in the destination

                    if (!Directory.Exists(destinationItem))
                    {
                        Directory.CreateDirectory(destinationItem);
                        Log.Information($"Created directory: {destinationItem}");
                    }
                }
            }

            // Remove items from destination that do not exist in source

            foreach (var destinationItem in destinationItems)
            {
                var relativePath = destinationItem.Substring(destinationFolderPath.Length + 1);
                var sourceItem = Path.Combine(sourceFolderPath, relativePath);

                if (!File.Exists(sourceItem) && !Directory.Exists(sourceItem))
                {
                    if (File.Exists(destinationItem))
                    {
                        File.Delete(destinationItem);
                        Log.Information($"Deleted file: {destinationItem}");
                    }
                    else if (Directory.Exists(destinationItem))
                    {
                        Directory.Delete(destinationItem, true);
                        Log.Information($"Deleted directory: {destinationItem}");
                    }
                }
            }

            Log.Information("Synchronization completed");
        }
    }

    // Function responsible for comparing content of two files to determine if they are identical or different
    private static bool FileCompare(string file1, string file2)
    {
        int file1byte;
        int file2byte;

        using (FileStream fs1 = new FileStream(file1, FileMode.Open))
        using (FileStream fs2 = new FileStream(file2, FileMode.Open))
        {
            do
            {
                file1byte = fs1.ReadByte();
                file2byte = fs2.ReadByte();
            } while (file1byte == file2byte && file1byte != -1);
        }

        return file1byte - file2byte == 0;
    }
}
