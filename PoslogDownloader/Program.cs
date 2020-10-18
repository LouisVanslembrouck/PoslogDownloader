using System;
using System.IO;
using Renci.SshNet;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using Renci.SshNet.Common;
using System.Xml;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO.Ports;

namespace PoslogDownloader
{
    class Program
    {

        public static void Main()
        {
            Console.WriteLine("Poslog Downloader - For instructions please consult the Readme file.");
            Console.WriteLine("Hit enter to continue...");
            Console.ReadLine();

            string cwd = Directory.GetCurrentDirectory();
            string input_file = Path.Combine(cwd, "poslog.txt");
            string output_file = Path.Combine(cwd, "output.txt");
            string output_dir = Path.Combine(cwd, "Copied");
            string search_dir = @"C:\Users\louis\Documents\Receipt_Backup\";
            //string user = "root";
            string user = "louisvanslembrouck@gmail.com";
            string password = "Botermans123";


            List<string> success = new List<string>();
            List<string> failed = new List<string>();

            foreach(var item in Retrieve_input(input_file))
            {
                // PROD Version: (var client = new SftpClient(item.Hostname, 22, user, Get_pwd(item.hostname))

                DateTime date = Convert.ToDateTime(item.Date);
                DateTime hour = Convert.ToDateTime(item.Hour);

                // Month has to be parsed as 2 digits if < 10

                string filePath = Path.Combine(search_dir, date.Year.ToString(), date.Month.ToString(), date.Day.ToString(), hour.Hour.ToString());
                string fileName = Path.Combine(search_dir, date.Year.ToString(), date.Month.ToString(), date.Day.ToString(), hour.Hour.ToString(), item.Id);


                using (var client = new SftpClient(item.Hostname, 22, user, password))
                {
                    try
                    {
                        client.Connect();

                        if(!client.Exists(fileName))
                        {
                            Console.WriteLine("File not found on remote host.");
                        }
                        else
                        {
                            using (Stream fileStream = File.OpenWrite(Path.Combine(output_dir, item.Id)))
                            {
                                client.DownloadFile(fileName, fileStream);
                            }

                            success.Add(item.ToString());
                        }
                    }
                    catch (Exception)
                    {
                        failed.Add(item.ToString());
                    }
                }
            }

            Console.WriteLine(failed);
            Console.WriteLine(success);
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

            // Method returns a list of Pairs to retrieve.

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
    }
}
