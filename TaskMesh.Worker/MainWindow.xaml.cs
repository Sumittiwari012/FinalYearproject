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
        private WorkerMainViewModel _viewModel;

        public MainWindow(string workerId, string workerName, string masterIp)
        {
            InitializeComponent();
            _viewModel = new WorkerMainViewModel(workerId, workerName, masterIp);
            DataContext = _viewModel;
            Loaded += async (s, e) => await _viewModel.ExecuteConnect();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            _viewModel.StartFocusMonitor(handle);
        }
    }
}