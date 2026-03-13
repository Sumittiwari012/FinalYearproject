using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
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
        public event Action OnResultUpdated;
        private List<ProblemTask> _fullProblems = new List<ProblemTask>();
        private Dictionary<string, HashSet<Guid>> _workerProblemSets = new Dictionary<string, HashSet<Guid>>();

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
        private ProblemViewModel _selectedProblem;
        public ProblemViewModel SelectedProblem
        {
            get => _selectedProblem;
            set => SetProperty(ref _selectedProblem, value);
        }
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
                _masterServer.OnWorkerRegistered += async (workerId, ipAddress, workerName, existingIds) =>
                {
                    System.Diagnostics.Debug.WriteLine($"Worker registered: {workerId}");
                    System.Diagnostics.Debug.WriteLine($"Full problems count: {_fullProblems.Count}");
                    System.Diagnostics.Debug.WriteLine($"Existing IDs from worker: {existingIds.Count}");
                    await Task.Delay(500);
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        if (!Workers.Any(w => w.WorkerId == workerId))
                        {
                            Workers.Add(new WorkerViewModel
                            {
                                WorkerId = workerId,
                                WorkerName = workerName,
                                IpAddress = ipAddress,
                                Status = WorkerStatus.Online
                            });
                            GetOrCreateEntry(workerId, workerName);
                            ScoreboardColumnsChanged?.Invoke();
                        }
                    });

                    // Initialize worker set from what they sent
                    _workerProblemSets[workerId] = new HashSet<Guid>(existingIds);

                    // Calculate missing = master set - worker set
                    var missingProblems = _fullProblems
                        .Where(p => !_workerProblemSets[workerId].Contains(p.ProblemId))
                        .ToList();

                    // Send only missing
                    foreach (var problem in missingProblems)
                    {
                        System.Diagnostics.Debug.WriteLine($"Dispatching: {problem.ProblemName} to {workerId}");
                        await _dispatcher.DispatchSingleToWorkerAsync(workerId, problem);
                        _workerProblemSets[workerId].Add(problem.ProblemId);
                    }
                };

                // When result arrives → update UI
                _masterServer.OnResultReceived +=async( result )=>
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        var problem = Problems.FirstOrDefault(p => p.ProblemId == result.ProblemId);
                        var worker = Workers.FirstOrDefault(w => w.WorkerId == result.WorkerId);

                        string problemName = problem?.ProblemName ?? "Unknown";
                        string studentName = worker?.WorkerName ?? result.WorkerId;

                        var entry = GetOrCreateEntry(result.WorkerId, studentName);
                        entry.SetResult(problemName, result.IsSuccess);
                        OnResultUpdated?.Invoke();
                        if (problem != null)
                        {
                            problem.TestCasePassCount = result.TestCasePassCount;
                            problem.TotalTestCaseCount = result.TotalTestCaseCount;
                            problem.Status = result.IsSuccess ?
                                ProblemStatus.Solved : ProblemStatus.Failed;
                        }
                    });
                    if (_workerProblemSets.ContainsKey(result.WorkerId))
                    {
                        var workerSet = _workerProblemSets[result.WorkerId];
                        var missingProblems = _fullProblems
                            .Where(p => !workerSet.Contains(p.ProblemId))
                            .ToList();

                        foreach (var problem in missingProblems)
                        {
                            await _dispatcher.DispatchSingleToWorkerAsync(
                                result.WorkerId, problem);
                            workerSet.Add(problem.ProblemId);
                        }
                    }
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

            // Just store it
            _fullProblems.Add(problem);
            _dispatcher.AddProblem(problem);

            // Add to UI
            Problems.Add(new ProblemViewModel
            {
                ProblemId = problem.ProblemId,
                ProblemName = problem.ProblemName,
                ProblemDescription = problem.ProblemDescription,
                InputTestCases = problem.InputTestCases,
                ExpectedOutputTestCases = problem.ExpectedOutputTestCases,
                TimeLimitSeconds = problem.TimeLimitSeconds,
                Status = ProblemStatus.Pending,
                TotalTestCaseCount = problem.InputTestCases.Count
            });

            ScoreboardColumnsChanged?.Invoke();
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