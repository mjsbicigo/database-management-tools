using System.Windows;
using System.Windows.Controls;

namespace SqlServerManagementTools.Views
{
    public partial class ConnectionWindowForm : Window
    {
        public string ServerName => ServerTextBox.Text;
        public string Username => UserTextBox.Text;
        public string Password => PasswordBox.Password;

        public ConnectionWindowForm()
        {
            InitializeComponent();
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ServerTextBox.Text))
            {
                MessageBox.Show("Type SQL Server name.");
                return;
            }

            this.DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
