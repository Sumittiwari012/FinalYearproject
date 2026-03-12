using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using TaskMesh.Core.Messages;
using TaskMesh.Core.Models;
using TaskMesh.Core.Network;
using TaskMesh.Master.Helper;
using TaskMesh.Master.Views;

namespace TaskMesh.Master.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // Services
        public event Action ScoreboardColumnsChanged;
        public ObservableCollection<ScoreboardEntry> Scoreboard { get; } = new();
        private MasterServer _masterServer = new MasterServer();
        private ProblemDispatcher _dispatcher;
        private bool _serverStarted = false;
        public bool CanStartServer => !_serverStarted;
        // Collections
        public ObservableCollection<string> Results { get; }
            = new ObservableCollection<string>();
        public ObservableCollection<WorkerViewModel> Workers { get; }
            = new ObservableCollection<WorkerViewModel>();
        public ObservableCollection<ProblemViewModel> Problems { get; }
            = new ObservableCollection<ProblemViewModel>();

        // Properties
        private string _serverStatus = "Server Stopped";
        public string ServerStatus
        {
            get => _serverStatus;
            set => SetProperty(ref _serverStatus, value);
        }

        // Commands
        public ICommand StartServerCommand { get; }
        public ICommand AddProblemCommand { get; }

        public MainViewModel()
        {
            StartServerCommand = new RelayCommand(() => ExecuteStartServer());
            AddProblemCommand = new RelayCommand(() => ExecuteAddProblem());
        }
        private ScoreboardEntry GetOrCreateEntry(string workerId, string workerName)
        {
            var entry = Scoreboard.FirstOrDefault(s => s.WorkerId == workerId);
            if (entry == null)
            {
                entry = new ScoreboardEntry
                {
                    WorkerId = workerId,
                    StudentName = workerName
                };
                App.Current.Dispatcher.Invoke(() => Scoreboard.Add(entry));
            }
            return entry;
        }
        private async void ExecuteStartServer()
        {
            try

            {
                if (_serverStarted) return;
                _serverStarted = true;
                // Initialize dispatcher
                _dispatcher = new ProblemDispatcher(_masterServer);

                // When worker registers → add to UI + dispatch problems
                _masterServer.OnWorkerRegistered += async (workerId, ipAddress,workerName) =>
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        if (!Workers.Any(w => w.WorkerId == workerId))
                        {
                            Workers.Add(new WorkerViewModel
                            {
                                WorkerId = workerId,
                                WorkerName = workerName,
                                IpAddress = ipAddress,
                                Status = WorkerStatus.Online,
                                CurrentLoad = 0
                            });
                            GetOrCreateEntry(workerId, workerName);

                            // Add pending columns for existing problems
                            ScoreboardColumnsChanged?.Invoke();
                        }
                    });

                    // Dispatch all current problems to this worker
                    foreach (var problem in Problems)
                    {
                        _dispatcher.AddProblem(new ProblemTask
                        {
                            ProblemId = problem.ProblemId,
                            ProblemName = problem.ProblemName
                        });
                    }
                    await _dispatcher.DispatchToWorkerAsync(workerId);
                };

                // When result arrives → update UI
                _masterServer.OnResultReceived += result =>
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        var problem = Problems.FirstOrDefault(p => p.ProblemId == result.ProblemId);
                        var worker = Workers.FirstOrDefault(w => w.WorkerId == result.WorkerId);

                        string problemName = problem?.ProblemName ?? "Unknown";
                        string studentName = worker?.WorkerName ?? result.WorkerId;

                        var entry = GetOrCreateEntry(result.WorkerId, studentName);
                        entry.SetResult(problemName, result.IsSuccess);

                        if (problem != null)
                        {
                            problem.TestCasePassCount = result.TestCasePassCount;
                            problem.TotalTestCaseCount = result.TotalTestCaseCount;
                            problem.Status = result.IsSuccess ?
                                ProblemStatus.Solved : ProblemStatus.Failed;
                        }
                    });
                };

                // Start server on background thread
                _ = Task.Run(() => _masterServer.StartListening());
                ServerStatus = "Server Running on Port 9000";
            }
            catch (Exception ex)
            {
                ServerStatus = $"Error: {ex.Message}";
            }
        }

        private async void ExecuteAddProblem()
        {
            var dialog = new AddProblemDialog();
            if (dialog.ShowDialog() != true) return;

            var problem = new ProblemTask
            {
                ProblemId = Guid.NewGuid(),
                ProblemName = dialog.ProblemTitle,
                ProblemDescription = dialog.ProblemDescription,
                InputTestCases = dialog.InputTestCases,
                ExpectedOutputTestCases = dialog.ExpectedOutputs,
                TimeLimitSeconds = dialog.TimeLimit
            };

            Problems.Add(new ProblemViewModel
            {
                ProblemId = problem.ProblemId,
                ProblemName = problem.ProblemName,
                Status = ProblemStatus.Pending,
                TotalTestCaseCount = problem.InputTestCases.Count
            });

            if (_dispatcher != null)
            {
                _dispatcher.AddProblem(problem);
                foreach (var worker in Workers)
                    await _dispatcher.DispatchToWorkerAsync(worker.WorkerId);
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void SetProperty<T>(ref T storage, T value,
            [CallerMemberName] string propertyName = null)
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