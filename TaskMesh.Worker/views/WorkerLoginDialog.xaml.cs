using System.Windows;

namespace TaskMesh.Worker.Views
{
    public partial class WorkerLoginDialog : Window
    {
        public string WorkerId { get; private set; }
        public string WorkerName { get; private set; }
        public string MasterIp { get; private set; }

        public WorkerLoginDialog() { InitializeComponent(); }

        private void JoinButton_Click(object sender, RoutedEventArgs e)
        {
            // Temporarily skip validation
            WorkerId = string.IsNullOrWhiteSpace(WorkerIdBox.Text) ? "Worker1" : WorkerIdBox.Text.Trim();
            WorkerName = string.IsNullOrWhiteSpace(NameBox.Text) ? "Student1" : NameBox.Text.Trim();
            MasterIp = string.IsNullOrWhiteSpace(MasterIpBox.Text) ? "127.0.0.1" : MasterIpBox.Text.Trim();
            DialogResult = true;
            Close();
        }
    }
}