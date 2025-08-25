using DatabaseManagementTools.Models;
using System;
using System.Diagnostics;
using System.IO;

namespace DatabaseManagementTools.Services
{
    public static class SqlPackageService
    {
        public static void Export(string sqlServerName, string userName, string password, string dbName, string outputFolder, Action<string> logCallback, string logFilePath)
        {
            string outputFile = Path.Combine(outputFolder, dbName + ".bacpac");

            string connectionString =
                $"Server={sqlServerName};Initial Catalog={dbName};User ID={userName};Password={password};Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;";

            string args = $"/Action:Export /TargetFile:\"{outputFile}\" /SourceConnectionString:\"{connectionString}\"";

            logCallback($"Exporting {dbName}");
            LogToFile(logFilePath, $"[{DateTime.Now}] Starting export of {dbName}");
            LogToFile(logFilePath, $"    SqlPackage.exe {args}");

            string output = RunSqlPackage(args);
            LogToFile(logFilePath, output);

            LogToFile(logFilePath, $"[{DateTime.Now}] Finished export of {dbName}");
        }

        public static void Import(string sqlServerName, string userName, string password, string bacpacFilePath, Action<string> logCallback, string logFilePath, bool isAzureSqlDatabaseServer)
        {
            string dbName = Path.GetFileNameWithoutExtension(bacpacFilePath);

            string connectionString =
                $"Server={sqlServerName};Initial Catalog={dbName};User ID={userName};Password={password};Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;";

            string argsAzureSqlTrue = $"/Action:Import /SourceFile:\"{bacpacFilePath}\" /TargetConnectionString:\"{connectionString}\" /p:DatabaseEdition=Basic /p:DatabaseServiceObjective=Basic";
            string argsAzureSqlFalse = $"/Action:Import /SourceFile:\"{bacpacFilePath}\" /TargetConnectionString:\"{connectionString}";

            if (isAzureSqlDatabaseServer)
            {
                logCallback($"Importing {dbName}");
                LogToFile(logFilePath, $"[{DateTime.Now}] Starting import of {dbName}");
                LogToFile(logFilePath, $"    SqlPackage.exe {argsAzureSqlTrue}");

                string output = RunSqlPackage(argsAzureSqlTrue);
                LogToFile(logFilePath, output);

                LogToFile(logFilePath, $"[{DateTime.Now}] Finished import of {dbName}");
            }
            else
            {
                logCallback($"Importing {dbName}");
                LogToFile(logFilePath, $"[{DateTime.Now}] Starting import of {dbName}");
                LogToFile(logFilePath, $"    SqlPackage.exe {argsAzureSqlFalse}");

                string output = RunSqlPackage(argsAzureSqlFalse);
                LogToFile(logFilePath, output);

                LogToFile(logFilePath, $"[{DateTime.Now}] Finished import of {dbName}");
            }
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
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return output + Environment.NewLine + error;
        }

        private static void LogToFile(string logFilePath, string message)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
            File.AppendAllText(logFilePath, message + Environment.NewLine);
        }

        public static string GetLogFilePath(string operationFolder, string operationType)
        {
            string logsRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", operationFolder);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"{operationType}_{timestamp}.txt";
            return Path.Combine(logsRoot, fileName);
        }
    }
}
