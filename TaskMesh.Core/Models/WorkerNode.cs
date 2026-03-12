using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMesh.Core.Models
{
    public enum CurrentStatus
    {
        Available,
        Busy,
        Offline
    }
    public class WorkerNode
    {
        public string WorkerId { get; set; }

        public string WorkerName { get; set; }
        public string IpAddress { get; set; }
        public CurrentStatus WorkStatus { get; set; }

        public int CurrentLoad { get; set; }
        public DateTime LastHeartbeat { get; set; }
        public WorkerNode()
        {
            WorkerId = Guid.NewGuid().ToString();
            WorkStatus = CurrentStatus.Available;
            LastHeartbeat = DateTime.UtcNow;
        }
    }
}
