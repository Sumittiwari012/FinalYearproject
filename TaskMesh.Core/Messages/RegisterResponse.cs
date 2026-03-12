using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMesh.Core.Models;

namespace TaskMesh.Core.Messages
{
    public class RegisterResponse
    {
        public bool IsSuccess { get; set; }

        public string WelcomeMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
