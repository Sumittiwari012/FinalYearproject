using System;
using System.Collections.Generic;

namespace TaskMesh.Core.Messages
{
    
    public class ProblemAssignment
    {
        
        public Guid ProblemId { get; set; }

        
        public string ProblemName { get; set; }

        
        public string ProblemDescription { get; set; }

        
        public List<string> InputTestCases { get; set; } = new List<string>();

        
        public List<string> ExpectedOutputTestCases { get; set; } = new List<string>();

        
        public int TimeLimitSeconds { get; set; }
    }
}