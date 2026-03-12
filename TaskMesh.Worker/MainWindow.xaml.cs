using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TaskMesh.Worker.ViewModels;

namespace TaskMesh.Worker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(string workerId, string workerName, string masterIp)
        {
            InitializeComponent();
            var viewModel = new WorkerMainViewModel(workerId, workerName, masterIp);
            DataContext = viewModel;

            // Wait for window to load before connecting
            Loaded += async (s, e) => await viewModel.ExecuteConnect();
        }
    }
}