using System;
using System.IO;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using Renci.SshNet.Common;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO.Ports;
using System.Data;

namespace PoslogDownloader
{
    class Program
    {

        public static void Main()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Poslog Downloader - For instructions please consult the Readme file." + "\n");

            // Initialize variables
            string cwd = Directory.GetCurrentDirectory();
            string input_file = Path.Combine(cwd, "poslog.txt");
            string output_file = Path.Combine(cwd, "output.txt");
            string output_dir = Path.Combine(cwd, "Copied");
            string search_dir = @"C:\Centric\Backup\OBP";
            string user = "root";
            string logfile = Path.Combine(cwd, "log.txt");


            // Check if output folder exists
            if (!Directory.Exists(output_dir))
            {
                Directory.CreateDirectory(output_dir);

                using (StreamWriter w = File.AppendText(logfile))
                {
                    w.WriteLine($"\nLog from last run on {DateTime.Now.ToLongDateString()} - {DateTime.Now.ToLongTimeString()}." + "\n");
                }
            }
            else
            {
                Console.WriteLine("Do you wish to remove previously found files from output directory? y/n");
                string output = Console.ReadLine();

                if (output == "y")
                {
                    Directory.Delete(output_dir, true);
                    Directory.CreateDirectory(output_dir);

                    using (StreamWriter w = File.AppendText(logfile))
                    {
                        w.WriteLine($"\nLog from last run on {DateTime.Now.ToLongDateString()} - {DateTime.Now.ToLongTimeString()}." + "\n");
                    }
                }
                else if (output == "n")
                {
                    using (StreamWriter w = File.AppendText(logfile))
                    {
                        w.WriteLine($"\nLog from last run on {DateTime.Now.ToLongDateString()} - {DateTime.Now.ToLongTimeString()}." + "\n");
                    }
                }
                else
                {
                    Console.WriteLine("No valid input recieved, output directory will not be removed.");

                    using (StreamWriter w = File.AppendText(logfile))
                    {
                        w.WriteLine($"\nLog from last run on {DateTime.Now.ToLongDateString()} - {DateTime.Now.ToLongTimeString()}." + "\n");
                    }
                }
            }



            foreach (var item in Retrieve_input(input_file))
            {
                DateTime date = Convert.ToDateTime(item.Date);
                DateTime hour = Convert.ToDateTime(item.Hour);
                string Month = date.Month.ToString();

                // Month has to be parsed as 2 digits if < 10
                if (Month.Length < 2)
                {
                    Month = "0" + Month;
                }

                string filePath = Path.GetFullPath(Path.Combine(search_dir, date.Year.ToString(), Month, date.Day.ToString(), hour.Hour.ToString()));
                string fileName = Path.Combine(search_dir, date.Year.ToString(), Month, date.Day.ToString(), hour.Hour.ToString(), item.Id);
                string localFile = Path.Combine(output_dir, item.Id.ToString());
                string password = Get_pwd(item.Hostname);


                using (var client = new SftpClient(item.Hostname, 22, user, password))
                {
                    try
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("\n" + $"Downloading {fileName} from {filePath} on {item.Hostname}...");

                        client.Connect();

                        var files = client.ListDirectory(filePath);
                        Console.WriteLine(files);

                        foreach (var file in files)
                        {
                            Console.WriteLine(file.FullName);
                        }

                        using (Stream file = File.OpenWrite(localFile))
                        {
                            client.DownloadFile(Path.GetFullPath(fileName), file);
                        }

                        Console.WriteLine($"Downloaded {item.Id} from {item.Hostname}.");
                    }

                    catch (Exception e)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Failed downloading {item.Id} from {item.Hostname}.");

                        // Log result to logfile.
                        using (StreamWriter error = File.AppendText(logfile))
                        {
                            error.WriteLine($"Failed Downloading {item.Id} in {filePath} from {item.Hostname} due to {e}.");
                        }
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n" + "Please check Copied folder in current directory for donwloaded files.");
            Console.WriteLine("Please check logfile for any failed files.");
            Console.WriteLine("\nPress enter to exit...");
            Console.ReadLine();
        }
        

        static string Get_pwd(string input)
        { 

            // Method returns password for root user based on hostname.

            string result = Regex.Replace(input, @"\D", "");
            return string.Concat("admin", result.Substring(0,4));
        }


        static List<Pair> Retrieve_input(string file)
        {

            // Method returns a list of Pairs (Filename, Hostname, Date, Hour) to retrieve.

            if (File.Exists(file))
            {
                List<string> readText = File.ReadAllLines(file).ToList();
                List<Pair> files = new List<Pair>();

                foreach (var line in readText)
                {
                    string[] entries = line.Split();

                    Pair Items = new Pair
                    {
                        Id = entries[0],
                        Hostname = entries[1],
                        Date = entries[2],
                        Hour = entries[3]
                    };
                    
                    files.Add(Items);
                }

                // !!! Very Sensitive to whitespace between words !!!
                return files;
            }
            else
            {
                Console.WriteLine("No poslog.txt input file was provided, created dummy file.");
                File.Create(file);

                List<Pair> empty = new List<Pair>();

                return empty;
            }
        }
    }
}
