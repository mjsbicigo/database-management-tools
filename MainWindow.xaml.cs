using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using DatabaseManagementTools.Helpers;
using DatabaseManagementTools.Models;
using DatabaseManagementTools.Services;
using DatabaseManagementTools.Views;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace DatabaseManagementTools
{
    public partial class MainWindow : Window
    {
        private string exportPath = string.Empty;
        private string importPath = string.Empty;

        private SqlConnectionManager currentExportSqlConnectionManager = null!;
        private string currentExportServerName = string.Empty;
        private string currentExportUser = string.Empty;
        private string currentExportPassword = string.Empty;

        private SqlConnectionManager currentImportSqlConnectionManager = null!;
        private string currentImportServerName = string.Empty;
        private string currentImportUser = string.Empty;
        private string currentImportPassword = string.Empty;

        public ObservableCollection<DatabaseItem> DatabaseExportItems { get; set; } = new();
        public ObservableCollection<DatabaseItem> DatabaseImportItems { get; set; } = new();

        public bool isAzureSqlDatabaseServer = false;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            if (!SqlPackageChecker.Exists())
                MessageBox.Show(SqlPackageChecker.GetHelpMessage(), "SqlPackage missing");
        }

        private void LogStatus(string message)
        {
            Dispatcher.Invoke(() => { StatusText.Text = message; });
            Debug.WriteLine(message);
        }

        private void OpenConnectionExportDialog_Click(object sender, RoutedEventArgs e)
        {
            OpenConnectionExportButton.IsEnabled = false;
            var connectionWindow = new ConnectionWindowForm { Owner = this };

            if (connectionWindow.ShowDialog() == true)
            {
                string exportServerName = connectionWindow.ServerName;
                string exportUser = connectionWindow.Username;
                string exportPassword = connectionWindow.Password;

                StatusText.Text = "Trying to connect to the server.";
                var exportSqlConnectionManager = new SqlConnectionManager(exportServerName, exportUser, exportPassword);

                try
                {
                    using var conn = exportSqlConnectionManager.GetOpenConnection();
                    var databases = DatabaseService.ListDatabases(conn);

                    DatabaseExportItems.Clear();
                    foreach (var db in databases)
                        DatabaseExportItems.Add(new DatabaseItem { Name = db });

                    StatusText.Text = "Databases listed successfully.";
                    ConnectedServerExportText.Text = exportServerName;

                    ConnectionStatusExportText.Text = "Connected";
                    ConnectionStatusExportText.Foreground = System.Windows.Media.Brushes.Green;

                    currentExportSqlConnectionManager = exportSqlConnectionManager;
                    currentExportServerName = exportServerName;
                    currentExportUser = exportUser;
                    currentExportPassword = exportPassword;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                    ConnectionStatusExportText.Text = "Connection Failed";
                    ConnectionStatusExportText.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            OpenConnectionExportButton.IsEnabled = true;
        }

        private void OpenConnectionImportDialog_Click(object sender, RoutedEventArgs e)
        {
            OpenConnectionImportButton.IsEnabled = false;
            var connectionWindow = new ConnectionWindowForm { Owner = this };

            if (connectionWindow.ShowDialog() == true)
            {
                string importServerName = connectionWindow.ServerName;
                string importUser = connectionWindow.Username;
                string importPassword = connectionWindow.Password;

                StatusText.Text = "Trying to connect to the server.";
                var importSqlConnectionManager = new SqlConnectionManager(importServerName, importUser, importPassword);

                try
                {
                    using var conn = importSqlConnectionManager.GetOpenConnection();
                    currentImportSqlConnectionManager = importSqlConnectionManager;
                    currentImportServerName = importServerName;
                    currentImportUser = importUser;
                    currentImportPassword = importPassword;

                    StatusText.Text = "Connection established successfully. Select the folder containing the .bacpac files.";
                    ConnectedServerImportText.Text = importServerName;

                    ConnectionStatusImportText.Text = "Connected";
                    ConnectionStatusImportText.Foreground = System.Windows.Media.Brushes.Green;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                    ConnectionStatusImportText.Text = "Connection Failed";
                    ConnectionStatusImportText.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            OpenConnectionImportButton.IsEnabled = true;
        }

        private void ChooseExportFolder_Click(object sender, RoutedEventArgs e)
        {
            ChooseExportFolderButton.IsEnabled = false;
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Select the folder where the .bacpac files will be saved",
                UseDescriptionForTitle = true
            };
            if (dialog.ShowDialog() == true)
            {
                exportPath = dialog.SelectedPath;
                ExportPathBox.Text = exportPath;
                ExportPathBox.Foreground = System.Windows.Media.Brushes.Black;
            }
            ChooseExportFolderButton.IsEnabled = true;
        }

        private async void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            ExportAllButton.IsEnabled = false;

            if (currentExportSqlConnectionManager == null || string.IsNullOrWhiteSpace(currentExportServerName))
            {
                MessageBox.Show("No server connected on Export tab. Click 'Connect Server...' first.");
                StatusText.Text = "No server connected on Export tab.";
                ExportAllButton.IsEnabled = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(exportPath))
            {
                MessageBox.Show("No export folder selected. Please choose a folder.");
                StatusText.Text = "No export folder selected.";
                ExportAllButton.IsEnabled = true;
                return;
            }

            var dbNames = DatabaseExportItems.Where(d => d.IsSelected).Select(d => d.Name).ToList();
            if (dbNames.Count == 0)
            {
                MessageBox.Show("No databases selected to export.");
                ExportAllButton.IsEnabled = true;
                return;
            }

            var logFilePath = SqlPackageService.GetLogFilePath("Exports", "export");

            await Task.Run(() =>
            {
                int total = dbNames.Count;
                int current = 0;

                foreach (string dbName in dbNames)
                {
                    try
                    {
                        SqlPackageService.Export(currentExportServerName, currentExportUser, currentExportPassword, dbName, exportPath, LogStatus, logFilePath);
                        current++;
                        Dispatcher.Invoke(() =>
                        {
                            OperationProgress.Value = (current * 100) / total;
                        });
                        LogStatus($"Export finished for {dbName}");
                    }
                    catch (Exception ex)
                    {
                        LogStatus($"Error exporting {dbName}: {ex.Message}");
                    }
                }
            });

            ExportAllButton.IsEnabled = true;
            MessageBox.Show("Export completed.");
        }

        private void ChooseImportFolder_Click(object sender, RoutedEventArgs e)
        {
            ChooseImportFolderButton.IsEnabled = false;
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Select the folder containing the .bacpac files",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == true)
            {
                importPath = dialog.SelectedPath;
                ImportPathBox.Text = importPath;
                ImportPathBox.Foreground = System.Windows.Media.Brushes.Black;

                var files = Directory.GetFiles(importPath, "*.bacpac");
                
                if (files.Length > 0)
                {
                    DatabaseImportItems.Clear();
                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileName(file);
                        DatabaseImportItems.Add(new DatabaseItem { Name = fileName, IsSelected = false });
                    }
                }
                else
                {
                    MessageBox.Show("No .bacpac files found in the selected folder.");
                    DatabaseImportItems.Clear();
                }
                StatusText.Text = "Select which .bacpac files to import, then click 'Import All'.";
            }
            ChooseImportFolderButton.IsEnabled = true;
        }

        private async void ImportAll_Click(object sender, RoutedEventArgs e)
        {
            ImportAllButton.IsEnabled = false;

            if (currentImportSqlConnectionManager == null || string.IsNullOrWhiteSpace(currentImportServerName))
            {
                MessageBox.Show("No server connected on Import tab. Click 'Connect Server...' first.");
                StatusText.Text = "No server connected on Import tab.";
                ImportAllButton.IsEnabled = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(importPath))
            {
                MessageBox.Show("No import folder selected.");
                StatusText.Text = "No import folder selected.";
                ImportAllButton.IsEnabled = true;
                return;
            }

            var bacpacPaths = DatabaseImportItems.Where(d => d.IsSelected)
                                                 .Select(d => Path.Combine(importPath, d.Name))
                                                 .ToList();
            if (bacpacPaths.Count == 0)
            {
                MessageBox.Show("No .bacpac files selected to import.");
                ImportAllButton.IsEnabled = true;
                return;
            }

            string databasesList = string.Join(Environment.NewLine, bacpacPaths.Select(p => Path.GetFileNameWithoutExtension(p)));
            string message = $"The following databases will be imported to the server '{currentImportServerName}':\n\n{databasesList}\n\nDo you want to continue?";
            var result = MessageBox.Show(message, "Confirm Import", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                StatusText.Text = "Import cancelled by user.";
                ImportAllButton.IsEnabled = true;
                return;
            }

            var logFilePath = SqlPackageService.GetLogFilePath("Imports", "import");
            await Task.Run(() =>
            {
                int total = bacpacPaths.Count;
                int current = 0;

                foreach (string bacpacPath in bacpacPaths)
                {
                    try
                    {
                        if (isAzureSqlDatabaseServer == true)
                            SqlPackageService.Import(currentImportServerName, currentImportUser, currentImportPassword, bacpacPath, LogStatus, logFilePath, true);
                        else
                            SqlPackageService.Import(currentImportServerName, currentImportUser, currentImportPassword, bacpacPath, LogStatus, logFilePath, false);

                        current++;
                        Dispatcher.Invoke(() =>
                        {
                            OperationProgress.Value = (current * 100) / total;
                        });
                        LogStatus($"Import finished for {bacpacPath}");
                    }
                    catch (Exception ex)
                    {
                        LogStatus($"Error importing {bacpacPath}: {ex.Message}");
                    }
                }
            });

            ImportAllButton.IsEnabled = true;
            StatusText.Text = "Import complete. Ready for another.";
            MessageBox.Show("Import completed.");
        }

        private void SelectAllDatabasesExportCheckBox_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = SelectAllDatabasesExportCheckBox.IsChecked == true;
            foreach (var item in DatabaseExportItems)
                item.IsSelected = isChecked;
        }

        private void SelectAllDatabasesImportCheckBox_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = SelectAllDatabasesImportCheckBox.IsChecked == true;
            foreach (var item in DatabaseImportItems)
                item.IsSelected = isChecked;
        }
        private void IsAzureDatabaseCheckBox_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = IsAzureDatabaseCheckBox.IsChecked == true;
            isAzureSqlDatabaseServer = isChecked;
        }
    }
}
