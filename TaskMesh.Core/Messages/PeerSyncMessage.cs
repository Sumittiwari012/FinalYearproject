using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMesh.Core.Messages
{
    public class PeerSyncMessage
    {
        public string SyncType { get; set; }
        // "WORKER_REGISTERED", "RESULT", "PROBLEM", "TAB_SWITCH"
        public string Payload { get; set; } // JSON of the actual message
        public string OriginMasterId { get; set; }
    }
}
