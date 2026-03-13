using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMesh.Core.Messages
{
    public class TabSwitchAlertMessage
    {
        public string WorkerId { get; set; }
        public DateTime AlertTime { get; set; }
    }
}
