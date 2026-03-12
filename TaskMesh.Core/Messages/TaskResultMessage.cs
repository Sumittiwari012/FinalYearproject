using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMesh.Core.Messages
{
    public class TaskResultMessage
    {
        public Guid TaskId { get; set; }
        public string WorkerId { get; set; }
        public double DurationSecond { get; set; }
        public string OutputFilePath { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        
    }
}
