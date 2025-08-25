using System.Windows;
using System.Windows.Controls;

namespace DatabaseManagementTools.Views
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
            if (string.IsNullOrWhiteSpace(ServerTextBox.Text) || string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("The SQL Server name and password cannot be blank.");
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
