using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using TaskMesh.Core.Execution;
using TaskMesh.Core.Messages;
using TaskMesh.Core.Models;
using System.Runtime.InteropServices;
using TaskMesh.Core.Network;
using TaskMesh.Worker.Helper; // Ensure this matches where your RelayCommand lives

namespace TaskMesh.Worker.ViewModels
{
    public class WorkerMainViewModel : INotifyPropertyChanged
    {
        private readonly WorkerClient _workerClient = new WorkerClient();
        private readonly ExecutionSandBox _sandbox = new ExecutionSandBox();
        public ICommand RefreshCommand { get; }

        // In constructor:
        private bool _canSubmit = true;
        public bool CanSubmit
        {
            get => _canSubmit;
            set
            {
                SetProperty(ref _canSubmit, value);
                App.Current.Dispatcher.Invoke(
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested);
            }
        }

        private async Task ExecuteRefresh()
        {
            var existingIds = Problems.Select(p => p.ProblemId).ToList();
            await _workerClient.ConnectAsync(MasterIp, WorkerId, WorkerName, existingIds);
        }
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private IntPtr _workerWindowHandle;
        private bool _sessionActive = false;

        public void StartFocusMonitor(IntPtr windowHandle)
        {
            _workerWindowHandle = windowHandle;
            _sessionActive = false; // ← don't start yet

            _ = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(500);

                    if (!_sessionActive) continue; // ← wait until session starts

                    var foreground = GetForegroundWindow();
                    if (foreground != _workerWindowHandle)
                    {
                        CanSubmit = false;
                        await _workerClient.SendTabSwitchAlertAsync();
                        await Task.Delay(5000);
                    }
                }
            });
        }
        public WorkerMainViewModel(string workerId, string workerName, string masterIp)
        {
            WorkerId = workerId;
            WorkerName = workerName;
            MasterIp = masterIp;
            RefreshCommand = new RelayCommand(
            async () => await ExecuteRefresh(),
            () => IsConnected);
            ConnectCommand = new RelayCommand(async () => await ExecuteConnect());
            SubmitSolutionCommand = new RelayCommand(
                async () => await ExecuteSubmit(),
                () => IsConnected && SelectedProblem != null);

            _workerClient.OnProblemReceived += problem =>
            {
                System.Diagnostics.Debug.WriteLine($"Problem received on worker: {problem.ProblemName}");
                App.Current.Dispatcher.Invoke(() =>
                {
                    if (!Problems.Any(p => p.ProblemId == problem.ProblemId))
                        Problems.Add(problem);
                });
            };
            _workerClient.OnSessionStartReceived += (minutes) =>
            {
                _sessionActive = true; // ← now start monitoring
                App.Current.Dispatcher.Invoke(() =>
                    ResultLog = $"⏱ Session started — {minutes} minutes");
            };
        }

        #region Properties

        public string WorkerId { get; private set; }
        public string WorkerName { get; private set; }

        private string _connectionStatus = "Disconnected";
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set
            {
                SetProperty(ref _connectionStatus, value);
                OnPropertyChanged(nameof(IsConnected)); // Notify dependent property
            }
        }

        public bool IsConnected => ConnectionStatus == "Connected";

        private string _masterIp = "127.0.0.1";
        public string MasterIp
        {
            get => _masterIp;
            set => SetProperty(ref _masterIp, value);
        }

        private ProblemAssignment _selectedProblem;
        public ProblemAssignment SelectedProblem
        {
            get => _selectedProblem;
            set => SetProperty(ref _selectedProblem, value);
        }

        private string _currentCode = "// Write your Java code here...";
        public string CurrentCode
        {
            get => _currentCode;
            set => SetProperty(ref _currentCode, value);
        }

        private string _resultLog;
        public string ResultLog
        {
            get => _resultLog;
            set => SetProperty(ref _resultLog, value);
        }

        #endregion

        public ObservableCollection<ProblemAssignment> Problems { get; } = new ObservableCollection<ProblemAssignment>();

        public ICommand ConnectCommand { get; }
        public ICommand SubmitSolutionCommand { get; }

        #region Execution Logic

        public async Task ExecuteConnect()
        {
            try
            {
                ConnectionStatus = "Connecting...";

                // Send existing problem IDs to master
                var existingIds = Problems
                    .Select(p => p.ProblemId)
                    .ToList();

                await _workerClient.ConnectAsync(MasterIp, WorkerId,
                    WorkerName, existingIds);
                ConnectionStatus = "Connected";
            }
            catch (Exception ex)
            {
                ConnectionStatus = "Connection Failed";
                ResultLog = $"Error: {ex.Message}";
            }
        }

        public async Task ExecuteSubmit()
        {
            if (SelectedProblem == null) return;

            ResultLog = "Compiling and Running...";

            // 1. Run the code locally in the Sandbox
            var result = await _sandbox.JudgeAsync(
                SelectedProblem.ProblemId,
                WorkerId,
                CurrentCode,
                SelectedProblem.InputTestCases,
                SelectedProblem.ExpectedOutputTestCases,
                SelectedProblem.TimeLimitSeconds);

            // 2. Update local feedback
            ResultLog = result.IsSuccess ?
                $"✅ Passed {result.TestCasePassCount}/{result.TotalTestCaseCount}" :
                $"❌ Failed — {result.ErrorMessage}";

            // 3. Report back to Master
            await _workerClient.SendResultAsync(new JudgeResultMessage
            {
                ProblemId = SelectedProblem.ProblemId,
                WorkerId = WorkerId,
                IsSuccess = result.IsSuccess,
                TestCasePassCount = result.TestCasePassCount,
                TotalTestCaseCount = result.TotalTestCaseCount,
                DurationSeconds = result.DurationSecond,
                SubmittedCode = CurrentCode,
                CompilationError = result.ErrorMessage // Assuming ErrorMessage holds compilation logs
            });
        }

        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return;
            storage = value;
            OnPropertyChanged(propertyName);
        }
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion
    }
}