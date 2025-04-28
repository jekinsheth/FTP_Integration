using System;
using System.IO;
using System.Linq;
using FluentFTP;
using Npgsql;
using Microsoft.Extensions.Configuration;
using System.IO;

class Program
{
    static async Task Main(string[] args)
    {
        // Load configuration
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var ftpHost = config["FTP:Host"];
        var ftpUser = config["FTP:Username"];
        var ftpPassword = config["FTP:Password"];
        var postgresConnectionString = config["PostgreSQL:ConnectionString"];

        try
        {
            using (var ftpClient = new FtpClient(ftpHost, ftpUser, ftpPassword))
            {
                ftpClient.Connect();

                var items = ftpClient.GetListing();

                foreach (var item in items)
                {
                    var fileInfo = ftpClient.GetObjectInfo(item.FullName);

                    if (fileInfo != null && fileInfo.Modified.Date == DateTime.UtcNow.Date)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(item.Name);
                        string extension = Path.GetExtension(item.Name);

                        Console.WriteLine($"Processing file: {fileName}, Extension: {extension}");

                        using (var conn = new NpgsqlConnection(postgresConnectionString))
                        {
                            conn.Open();

                            using (var cmd = new NpgsqlCommand("SELECT your_function_name(@file_name, @file_extension);", conn))
                            {
                                cmd.Parameters.AddWithValue("file_name", fileName);
                                cmd.Parameters.AddWithValue("file_extension", extension);

                                var result = cmd.ExecuteScalar();
                                Console.WriteLine($"Function result: {result}");
                            }
                        }
                    }
                }

                ftpClient.Disconnect();
            }
            Console.WriteLine("Process Completed.");
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
}

