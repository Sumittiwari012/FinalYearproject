using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TaskMesh.Master.ViewModels
{
    public enum ProblemStatus
    {
        Pending,
        Solved,
        Failed
    }

    public class ProblemViewModel : INotifyPropertyChanged
    {
        private Guid _problemId;
        private string _problemName;
        private int _totalTestCaseCount;
        private int _testCasePassCount;
        private ProblemStatus _status;
        public string ProblemDescription { get; set; }
        public List<string> InputTestCases { get; set; } = new();
        public List<string> ExpectedOutputTestCases { get; set; } = new();
        public long TimeLimitSeconds { get; set; }

        
        public Guid ProblemId
        {
            get => _problemId;
            set => SetProperty(ref _problemId, value);
        }

        public string ProblemName
        {
            get => _problemName;
            set => SetProperty(ref _problemName, value);
        }

        public int TotalTestCaseCount
        {
            get => _totalTestCaseCount;
            set => SetProperty(ref _totalTestCaseCount, value);
        }

        public int TestCasePassCount
        {
            get => _testCasePassCount;
            set => SetProperty(ref _testCasePassCount, value);
        }

        public ProblemStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return;

            storage = value;
            OnPropertyChanged(propertyName);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}