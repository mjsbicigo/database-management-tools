using SqlServerManagementTools.Models;
using System;
using System.Diagnostics;
using System.IO;

namespace SqlServerManagementTools.Services
{
    public static class SqlPackageService
    {
        public static void Export(string sqlServerName, string userName, string password, string dbName, string outputFolder, Action<string> logCallback)
        {
            string outputFile = Path.Combine(outputFolder, dbName + ".bacpac");

            // Connection string para exportação
            string connectionString =
                $"Server={sqlServerName};Initial Catalog={dbName};User ID={userName};Password={password};Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;";

            string args = $"/Action:Export /TargetFile:\"{outputFile}\" /SourceConnectionString:\"{connectionString}\"";
            string logFilePath = GetLogFilePath("Exports", "export");

            logCallback($" {dbName}");
            LogToFile(logFilePath, $"[{DateTime.Now}] Exporting {dbName}");
            LogToFile(logFilePath, $"    SqlPackage.exe {args}");

            string output = RunSqlPackage(args);
            LogToFile(logFilePath, output);
        }

        public static void Import(string sqlServerName, string userName, string password, string bacpacFilePath, Action<string> logCallback)
        {
            string dbName = Path.GetFileNameWithoutExtension(bacpacFilePath);

            // Connection string para importação
            string connectionString =
                $"Server={sqlServerName};Initial Catalog={dbName};User ID={userName};Password={password};Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;";

            string args = $"/Action:Import /SourceFile:\"{bacpacFilePath}\" /TargetConnectionString:\"{connectionString}\" /p:DatabaseEdition=Basic /p:DatabaseServiceObjective=Basic";
            string logFilePath = GetLogFilePath("Imports", "import");

            logCallback($"Importing {dbName}");
            LogToFile(logFilePath, $"[{DateTime.Now}] Importing {dbName}");
            LogToFile(logFilePath, $"    SqlPackage.exe {args}");

            string output = RunSqlPackage(args);
            LogToFile(logFilePath, output);
        }

        private static string RunSqlPackage(string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "SqlPackage.exe",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true, // Adicione esta linha
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd(); // Capture o erro
            process.WaitForExit();

            return output + Environment.NewLine + error; // Inclua o erro no retorno
        }

        private static void LogToFile(string logFilePath, string message)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
            File.AppendAllText(logFilePath, message + Environment.NewLine);
        }

        private static string GetLogFilePath(string operationFolder, string operationType)
        {
            string logsRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", operationFolder);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"{operationType}_{timestamp}.txt";
            return Path.Combine(logsRoot, fileName);
        }
    }
}
