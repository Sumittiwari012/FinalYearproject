using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMesh.Core.Messages
{
    public class MasterPeerInfo
    {
        public string MasterId { get; set; }
        public string IpAddress { get; set; }
        public int Port { get; set; } = 9001;
    }
}
