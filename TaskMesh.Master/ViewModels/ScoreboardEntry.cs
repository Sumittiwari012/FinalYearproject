using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TaskMesh.Master.ViewModels
{
    public class ScoreboardEntry : INotifyPropertyChanged
    {
        public string WorkerId { get; set; }
        public string StudentName { get; set; }

        // Key = ProblemName, Value = "⏳" / "✅" / "❌"
        private Dictionary<string, string> _problemResults = new();

        public string GetResult(string problemName)
        {
            return _problemResults.TryGetValue(problemName, out var r) ? r : "⏳";
        }

        public void SetResult(string problemName, bool isSuccess)
        {
            _problemResults[problemName] = isSuccess ? "✅" : "❌";
            OnPropertyChanged(problemName);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}