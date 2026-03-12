using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMesh.Core.Models
{
    
    public enum Status
    {
        Pending,
        InProgress,
        Completed,
        Failed
    }
    public class CompressionTask
    {
        public Guid TaskId { get; set; }
        public List<string> FilePaths { get; set; }

        public string WorkerId { get; set; }

        public Status Status { get; set; }

        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public CompressionTask()
        {
            TaskId = Guid.NewGuid();
            FilePaths = new List<string>();
            Status = Status.Pending;
        }
    }

}
