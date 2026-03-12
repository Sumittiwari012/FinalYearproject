using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMesh.Core.Models;

namespace TaskMesh.Core.Messages
{
    public class RegisterRequest
    {
        public string WorkerId { get; set; }

        public string WorkerName { get; set; }

        public string IpAddress { get; set; }

        public int CurrentLoad { get; set; }

        public CurrentStatus WorkStatus { get; set; }
    }
}
