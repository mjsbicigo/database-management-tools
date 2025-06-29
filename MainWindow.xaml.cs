using Microsoft.Data.SqlClient;
using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using SqlServerManagementTools.Helpers;
using SqlServerManagementTools.Models;
using SqlServerManagementTools.Services;
using SqlServerManagementTools.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SqlServerManagementTools
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

        public ObservableCollection<DatabaseItem> DatabaseItems { get; set; } = new();

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            if (!SqlPackageChecker.Exists())
                MessageBox.Show(SqlPackageChecker.GetHelpMessage(), "SqlPackage missing");
        }

        private void LogStatus(string message)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = message;
            });
            Debug.WriteLine(message);
        }

        // Abre a janela de conexão para Exportação
        private void OpenConnectionExportDialog_Click(object sender, RoutedEventArgs e)
        {
            // Desabilita o botão para evitar múltiplos cliques
            OpenConnectionExportButton.IsEnabled = false;

            var connectionWindow = new ConnectionWindowForm
            {
                Owner = this
            };

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

                    DatabaseItems.Clear();
                    foreach (var db in databases)
                        DatabaseItems.Add(new DatabaseItem { Name = db });

                    //DatabaseList.Items.Clear();
                    //foreach (var db in databases)
                    //    DatabaseList.Items.Add(db);

                    StatusText.Text = "Databases listed successfully.";
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

        // Abre a janela de conexão para Importação
        private void OpenConnectionImportDialog_Click(object sender, RoutedEventArgs e)
        {
            // Desabilita o botão para evitar múltiplos cliques
            OpenConnectionImportButton.IsEnabled = false;

            var connectionWindow = new ConnectionWindowForm
            {
                Owner = this
            };

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
            // Desabilita o botão para evitar múltiplos cliques
            ChooseExportFolderButton.IsEnabled = false;
            
            var dialog = new VistaFolderBrowserDialog()
            {
                Description = "Select the folder where the .bacpac files will be saved",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == true)
            {
                exportPath = dialog.SelectedPath;

                if (ExportPathBox != null)
                {
                    ExportPathBox.Text = exportPath;
                    ExportPathBox.Foreground.Equals(System.Windows.Media.Brushes.Black);
                }
            }
            
            ChooseExportFolderButton.IsEnabled = true;
        }

        private async void ExportSelected_Click(object sender, RoutedEventArgs e)
        {
            ExportSelectedButton.IsEnabled = false;

            if (currentExportSqlConnectionManager == null || string.IsNullOrWhiteSpace(currentExportServerName))
            {
                MessageBox.Show("No server connected on Export tab. Click 'Connect Server...' first.");
                StatusText.Text = "No server connected on Export tab.";
                ExportSelectedButton.IsEnabled = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(exportPath))
            {
                MessageBox.Show("No export folder selected. Please choose a folder.");
                StatusText.Text = "No export folder selected.";
                ExportSelectedButton.IsEnabled = true;
                return;
            }

            var dbNames = DatabaseItems.Where(d => d.IsSelected).Select(d => d.Name).ToList();
            if (dbNames.Count == 0)
            {
                MessageBox.Show("No databases selected to export.");
                ExportSelectedButton.IsEnabled = true;
                return;
            }

            await Task.Run(() =>
            {
                foreach (string dbName in dbNames)
                {
                    try
                    {
                        SqlPackageService.Export(currentExportServerName, currentExportUser, currentExportPassword, dbName, exportPath, LogStatus);
                        LogStatus($"Export finished for {dbName}");
                    }
                    catch (Exception ex)
                    {
                        LogStatus($"Error exporting {dbName}: {ex.Message}");
                    }
                }
            });

            ExportSelectedButton.IsEnabled = true;
            MessageBox.Show("Export completed.");
        }

        private void ChooseImportFolder_Click(object sender, RoutedEventArgs e)
        {
            // Desabilita o botão para evitar múltiplos cliques
            ChooseImportFolderButton.IsEnabled = false;

            var dialog = new OpenFileDialog()
            {
                Filter = "bacpac files (*.bacpac)|*.bacpac",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                BacpacList.Items.Clear();
                foreach (var file in dialog.FileNames)
                    BacpacList.Items.Add(file);
            }

            ImportPathBox.Foreground.Equals(System.Windows.Media.Brushes.Black);

            ChooseImportFolderButton.IsEnabled = true;

            StatusText.Text = "Click 'Import All' to import selected .bacpac files.";
        }

        private async void ImportAll_Click(object sender, RoutedEventArgs e)
        {
            ImportSelectedButton.IsEnabled = false;

            if (currentImportSqlConnectionManager == null || string.IsNullOrWhiteSpace(currentImportServerName))
            {
                MessageBox.Show("No server connected on Import tab. Click 'Connect Server...' first.");
                StatusText.Text = "No server connected on Import tab. Click 'Connect Server...' first.";
                ImportSelectedButton.IsEnabled = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(importPath))
            {
                MessageBox.Show("No import folder selected. Please choose a folder.");
                StatusText.Text = "No import folder selected. Please choose a folder.";
                ImportSelectedButton.IsEnabled = true;
                return;
            }

            if (BacpacList.Items.Count == 0)
            {
                MessageBox.Show("No .bacpac files selected for import. Select at least one.");
                ImportSelectedButton.IsEnabled = true;
                return;
            }

            // Copie os caminhos dos arquivos para uma lista local
            var bacpacPaths = BacpacList.Items.Cast<string>().ToList();

            await Task.Run(() =>
            {
                foreach (string bacpacPath in bacpacPaths)
                {
                    try
                    {
                        SqlPackageService.Import(currentImportServerName, currentImportUser, currentImportPassword, bacpacPath, LogStatus);
                        LogStatus($"Import finished for {bacpacPath}");
                    }
                    catch (Exception ex)
                    {
                        LogStatus($"Error importing {bacpacPath}: {ex.Message}");
                    }
                }
            });

            ImportSelectedButton.IsEnabled = true;
            StatusText.Text = "Import complete. Ready to another.";
            MessageBox.Show("Import complete.");
        }

        private void SelectAllDatabasesCheckBox_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = SelectAllDatabasesCheckBox.IsChecked == true;
            foreach (var item in DatabaseItems)
                item.IsSelected = isChecked;
        }
    }
}
