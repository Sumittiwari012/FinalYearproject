using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TaskMesh.Master.ViewModels
{
    public enum WorkerStatus
    {
        Offline,
        Online
    }

    public class WorkerViewModel : INotifyPropertyChanged
    {
        private string _workerId;
        private string _ipAddress;
        private WorkerStatus _status;
        private int _currentLoad;
        private string _workerName;
        public string WorkerId
        {
            get => _workerId;
            set => SetProperty(ref _workerId, value);
        }
        public string WorkerName
        {
            get => _workerName;
            set => SetProperty(ref _workerName, value);
        }

        public string IpAddress
        {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        public WorkerStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public int CurrentLoad
        {
            get => _currentLoad;
            set => SetProperty(ref _currentLoad, value);
        }

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Helper method to update the property and notify the UI.
        /// </summary>
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