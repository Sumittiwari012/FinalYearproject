using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMesh.Core.Models
{
    public class JudgeResult
    {
        public Guid ResultId { get; set; }
        public Guid ProblemId { get; set; }
        public string WorkerId { get; set; }

        public int TestCasePassCount { get; set; }

        public int TotalTestCaseCount { get; set; }

        public double DurationSecond { get; set; }

        public string SubmitCode { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public JudgeResult()
        {
            SubmitCode = string.Empty;
        }
    }
}
