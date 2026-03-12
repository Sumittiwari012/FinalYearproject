using System.Configuration;
using System.Data;
using System.Windows;
using TaskMesh.Worker.Views;

namespace TaskMesh.Worker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Prevent auto-shutdown when login dialog closes
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var login = new WorkerLoginDialog();
            if (login.ShowDialog() != true)
            {
                Shutdown();
                return;
            }

            var mainWindow = new MainWindow(
                login.WorkerId,
                login.WorkerName,
                login.MasterIp);

            // Now switch back to normal shutdown mode
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            mainWindow.Show();
        }
    }

}
