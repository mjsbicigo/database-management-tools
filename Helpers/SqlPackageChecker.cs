using System.IO;

namespace DatabaseManagementTools.Helpers
{
    public static class SqlPackageChecker
    {
        public static bool Exists()
        {
            var path = Environment.GetEnvironmentVariable("PATH");
            return path.Split(';').Any(p => File.Exists(Path.Combine(p, "SqlPackage.exe")));
        }

        public static string GetHelpMessage()
        {
            return "SqlPackage.exe não encontrado. Baixe em: https://learn.microsoft.com/sql/tools/sqlpackage-download";
        }
    }
}
