using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMesh.Core.Models
{
    public enum ProblemStatus
    {
        Pending,
        Running,
        Completed,
        Failed
    }
    public class ProblemTask
    {
        public Guid ProblemId { get; set; }
        public string ProblemName { get; set; }
        public string ProblemDescription { get; set; }

        public List<string> InputTestCases { get; set; }
        public List<string> ExpectedOutputTestCases { get; set; }

        public long TimeLimitSeconds { get; set; }

        public string WorkerID { get; set; }

        public ProblemStatus Status { get; set; }

        public ProblemTask()
        {
            ProblemId = Guid.NewGuid();
            InputTestCases = new List<string>();
            ExpectedOutputTestCases = new List<string>();
            Status = ProblemStatus.Pending;
        }


    }
}
