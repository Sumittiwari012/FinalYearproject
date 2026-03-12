

namespace TaskMesh.Core.Models
{
    public class TaskResult
    {
        public  Guid TaskId { get; set; }
        public  string WorkerId { get; set; }

        public double DurationSecond { get; set; }
        public  List<string> OutputFilePaths { get; set; }

        public bool IsSuccess { get; set; }

        public string? ErrorMessage { get; set; }

       public TaskResult()
        {
            OutputFilePaths = new List<string>();
        }

    }
}
