using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
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
        private MasterPeerServer _peerServer;                                    // ← ADD
        private string _masterId = Guid.NewGuid().ToString()[..8];               // ← ADD

        public event Action ScoreboardColumnsChanged;
        public ObservableCollection<ScoreboardEntry> Scoreboard { get; } = new();
        private MasterServer _masterServer = new MasterServer();
        private ProblemDispatcher _dispatcher;
        private bool _serverStarted = false;
        private bool _sessionActive = false;
        private int _sessionRemainingSeconds = 0;
        public bool CanStartServer => !_serverStarted;

        public ObservableCollection<string> Results { get; } = new();
        public ObservableCollection<WorkerViewModel> Workers { get; } = new();
        public ObservableCollection<ProblemViewModel> Problems { get; } = new();

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

        public ICommand StartServerCommand { get; }
        public ICommand AddProblemCommand { get; }
        public ICommand StartSessionCommand { get; }

        public MainViewModel()
        {
            StartServerCommand = new RelayCommand(() => ExecuteStartServer());
            AddProblemCommand = new RelayCommand(() => ExecuteAddProblem());
            StartSessionCommand = new RelayCommand(() => ExecuteStartSession());
        }

        // ── ADD PEER ────────────────────────────────────────────────────────
        public void AddPeer(MasterPeerInfo peer)
        {
            _masterServer.AddPeer(peer);
        }
        private async Task SendFullStateToPeerAsync(string peerIp)
        {
            // Build full state snapshot
            var state = new
            {
                Problems = _fullProblems,
                Workers = Workers.Select(w => new
                {
                    w.WorkerId,
                    w.WorkerName,
                    w.IpAddress,
                    ExistingProblemIds = _workerProblemSets
                        .ContainsKey(w.WorkerId)
                            ? _workerProblemSets[w.WorkerId].ToList()
                            : new List<Guid>()
                }),
                Results = Scoreboard.Select(e => new
                {
                    e.WorkerId,
                    e.StudentName,
                    e.IsFlagged
                })
            };

            await _peerServer.BroadcastAsync(new PeerSyncMessage
            {
                SyncType = "STATE_SYNC",
                Payload = JsonSerializer.Serialize(state),
                OriginMasterId = _masterId
            });
        }
        public async Task ConnectToPeerAsync(string ip)
        {
            if (_peerServer == null)
            {
                MessageBox.Show("Please start the server first before adding peers.",
                    "Server Not Started", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await _peerServer.ConnectToPeerAsync(ip);

            // ← Send full current state to new peer
            await SendFullStateToPeerAsync(ip);
        }
        // ────────────────────────────────────────────────────────────────────

        private async void ExecuteStartSession()
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter session duration (minutes):", "Start Session", "60");
            if (!int.TryParse(input, out int minutes)) return;

            _sessionActive = true;
            _sessionRemainingSeconds = minutes * 60;

            await _masterServer.SendSessionStartAsync(minutes);
            ServerStatus = $"Session started — {minutes} min";

            // ← ADD countdown
            _ = Task.Run(async () =>
            {
                while (_sessionRemainingSeconds > 0)
                {
                    await Task.Delay(1000);
                    _sessionRemainingSeconds--;
                }
                _sessionActive = false;
                App.Current.Dispatcher.Invoke(() =>
                    ServerStatus = "Session Ended");
                await _masterServer.SendSessionEndAsync();
            });
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

                // ── PEER SERVER SETUP ────────────────────────────────────────
                _peerServer = new MasterPeerServer(_masterId);
                _ = Task.Run(() => _peerServer.StartListeningAsync());

                _peerServer.OnSyncReceived += msg =>
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        if (msg.SyncType == "RESULT")
                        {
                            var result = JsonSerializer
                                .Deserialize<JudgeResultMessage>(msg.Payload);

                            // ← ADD: sync problem set on this peer
                            if (!_workerProblemSets.ContainsKey(result.WorkerId))
                                _workerProblemSets[result.WorkerId] = new HashSet<Guid>();
                            _workerProblemSets[result.WorkerId].Add(result.ProblemId);

                            var problem = Problems
                                .FirstOrDefault(p => p.ProblemId == result.ProblemId);
                            var worker = Workers
                                .FirstOrDefault(w => w.WorkerId == result.WorkerId);
                            string problemName = problem?.ProblemName ?? "Unknown";
                            string studentName = worker?.WorkerName ?? result.WorkerId;
                            var entry = GetOrCreateEntry(result.WorkerId, studentName);
                            entry.SetResult(problemName, result.IsSuccess);
                            OnResultUpdated?.Invoke();
                        }
                        else if (msg.SyncType == "WORKER_REGISTERED")
                        {
                            // ← REPLACE old WorkerViewModel deserialize with this:
                            var data = JsonSerializer.Deserialize<JsonElement>(msg.Payload);
                            string workerId = data.GetProperty("WorkerId").GetString();
                            string workerName = data.GetProperty("WorkerName").GetString();
                            string ipAddress = data.GetProperty("IpAddress").GetString();
                            var existingIds = data.GetProperty("ExistingProblemIds")
                                .EnumerateArray()
                                .Select(x => Guid.Parse(x.GetString()))
                                .ToList();

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

                            // ← KEY: sync problem set to this peer
                            _workerProblemSets[workerId] = new HashSet<Guid>(existingIds);
                        }
                        else if (msg.SyncType == "TAB_SWITCH")
                        {
                            var entry = Scoreboard
                                .FirstOrDefault(s => s.WorkerId == msg.Payload);
                            if (entry != null) entry.IsFlagged = true;
                            OnResultUpdated?.Invoke();
                        }
                        else if (msg.SyncType == "PROBLEM")
                        {
                            var problem = JsonSerializer
                                .Deserialize<ProblemTask>(msg.Payload);
                            if (!_fullProblems.Any(p => p.ProblemId == problem.ProblemId))
                            {
                                _fullProblems.Add(problem);
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
                        }
                        else if (msg.SyncType == "STATE_SYNC")
                        {
                            var data = JsonSerializer.Deserialize<JsonElement>(msg.Payload);

                            // Restore problems
                            foreach (var p in data.GetProperty("Problems").EnumerateArray())
                            {
                                var problem = JsonSerializer.Deserialize<ProblemTask>(p.GetRawText());
                                if (!_fullProblems.Any(x => x.ProblemId == problem.ProblemId))
                                {
                                    _fullProblems.Add(problem);
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
                            }

                            // Restore workers
                            foreach (var w in data.GetProperty("Workers").EnumerateArray())
                            {
                                string workerId = w.GetProperty("WorkerId").GetString();
                                string workerName = w.GetProperty("WorkerName").GetString();
                                string ipAddress = w.GetProperty("IpAddress").GetString();
                                var existingIds = w.GetProperty("ExistingProblemIds")
                                    .EnumerateArray()
                                    .Select(x => Guid.Parse(x.GetString()))
                                    .ToList();

                                if (!Workers.Any(x => x.WorkerId == workerId))
                                {
                                    Workers.Add(new WorkerViewModel
                                    {
                                        WorkerId = workerId,
                                        WorkerName = workerName,
                                        IpAddress = ipAddress,
                                        Status = WorkerStatus.Offline // they were on another master
                                    });
                                    GetOrCreateEntry(workerId, workerName);
                                    ScoreboardColumnsChanged?.Invoke();
                                }
                                _workerProblemSets[workerId] = new HashSet<Guid>(existingIds);
                            }

                            // Restore flagged students
                            foreach (var r in data.GetProperty("Results").EnumerateArray())
                            {
                                string workerId = r.GetProperty("WorkerId").GetString();
                                bool isFlagged = r.GetProperty("IsFlagged").GetBoolean();
                                var entry = Scoreboard.FirstOrDefault(s => s.WorkerId == workerId);
                                if (entry != null && isFlagged)
                                    entry.IsFlagged = true;
                            }

                            OnResultUpdated?.Invoke();
                        }
                    });
                };
                // ─────────────────────────────────────────────────────────────

                // Tab switch alert
                _masterServer.OnTabSwitchAlert += workerId =>
                {
                    System.Diagnostics.Debug.WriteLine($"TAB SWITCH ALERT: {workerId}");
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        var entry = Scoreboard.FirstOrDefault(s => s.WorkerId == workerId);
                        if (entry != null) entry.IsFlagged = true;
                        OnResultUpdated?.Invoke();
                    });

                    // Broadcast to peers
                    _ = _peerServer.BroadcastAsync(new PeerSyncMessage
                    {
                        SyncType = "TAB_SWITCH",
                        Payload = workerId,
                        OriginMasterId = _masterId
                    });
                };

                _dispatcher = new ProblemDispatcher(_masterServer);

                // Worker registered
                _masterServer.OnWorkerRegistered += async (workerId, ipAddress, workerName, existingIds) =>
                {
                    System.Diagnostics.Debug.WriteLine($"Worker registered: {workerId}");
                    System.Diagnostics.Debug.WriteLine($"Full problems count: {_fullProblems.Count}");
                    System.Diagnostics.Debug.WriteLine($"Existing IDs from worker: {existingIds.Count}");

                    await Task.Delay(500);
                    _workerProblemSets[workerId] = new HashSet<Guid>(existingIds);

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        if (!Workers.Any(w => w.WorkerId == workerId))
                        {
                            var newWorker = new WorkerViewModel
                            {
                                WorkerId = workerId,
                                WorkerName = workerName,
                                IpAddress = ipAddress,
                                Status = WorkerStatus.Online
                            };
                            Workers.Add(newWorker);
                            GetOrCreateEntry(workerId, workerName);
                            ScoreboardColumnsChanged?.Invoke();

                            // Broadcast worker registered to peers
                            // Broadcast worker registered to peers — include problem set
                            _ = _peerServer.BroadcastAsync(new PeerSyncMessage
                            {
                                SyncType = "WORKER_REGISTERED",
                                Payload = JsonSerializer.Serialize(new
                                {
                                    WorkerId = workerId,
                                    WorkerName = workerName,
                                    IpAddress = ipAddress,
                                    ExistingProblemIds = _workerProblemSets[workerId].ToList()
                                }),
                                OriginMasterId = _masterId
                            });
                        }


                    });

                    var missingProblems = _fullProblems
                        .Where(p => !_workerProblemSets[workerId].Contains(p.ProblemId))
                        .ToList();

                    foreach (var problem in missingProblems)
                    {
                        System.Diagnostics.Debug.WriteLine($"Dispatching: {problem.ProblemName} to {workerId}");
                        await _dispatcher.DispatchSingleToWorkerAsync(workerId, problem);
                        _workerProblemSets[workerId].Add(problem.ProblemId);
                    }
                    if (_sessionActive && _sessionRemainingSeconds > 0)
                    {
                        await _masterServer.SendSessionStartToWorkerAsync(
                            workerId, _sessionRemainingSeconds / 60);
                    }

                };

                // Result received
                _masterServer.OnResultReceived += async (result) =>
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
                            problem.Status = result.IsSuccess
                                ? ProblemStatus.Solved
                                : ProblemStatus.Failed;
                        }
                    });

                    // Broadcast result to peers
                    await _peerServer.BroadcastAsync(new PeerSyncMessage
                    {
                        SyncType = "RESULT",
                        Payload = JsonSerializer.Serialize(result),
                        OriginMasterId = _masterId
                    });

                    // Send missing problems to worker
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

            _fullProblems.Add(problem);
            _dispatcher.AddProblem(problem);

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

            // Broadcast new problem to all peers
            await _peerServer.BroadcastAsync(new PeerSyncMessage
            {
                SyncType = "PROBLEM",
                Payload = JsonSerializer.Serialize(problem),
                OriginMasterId = _masterId
            });
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