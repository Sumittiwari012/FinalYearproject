using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMesh.Core.Messages
{
    public class BackupMasterList
    {
        public List<MasterPeerInfo> Peers { get; set; } = new();
    }
}
