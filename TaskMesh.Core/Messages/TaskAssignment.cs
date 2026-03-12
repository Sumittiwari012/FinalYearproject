using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMesh.Core.Messages
{
    public class TaskAssignment
    {
        public Guid TaskId { get; set; }
        
        public List<string> InputFilePaths { get; set; }

        public string OutputFolderPath { get; set; }
        public TaskAssignment()
        {
            InputFilePaths = new List<string>();
        }
    }
}
