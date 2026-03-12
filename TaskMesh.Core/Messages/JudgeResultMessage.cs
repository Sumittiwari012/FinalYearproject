using System;

namespace TaskMesh.Core.Messages
{
    
    public class JudgeResultMessage
    {
        
        public Guid ProblemId { get; set; }

        
        public string WorkerId { get; set; }

        public string WorkerName { get; set; }


        public bool IsSuccess { get; set; }

        
        public int TestCasePassCount { get; set; }

        
        public int TotalTestCaseCount { get; set; }

        public double DurationSeconds { get; set; }

        public string SubmittedCode { get; set; }

        public string? CompilationError { get; set; }
    }
}