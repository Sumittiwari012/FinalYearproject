using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TaskMesh.Core.Models;

namespace TaskMesh.Core.Network
{
    public class HeartbeatMonitor
    {
        const int HeartbeatPort = 9999;
        const int HeartbeatInterval = 3;    // seconds
        const int DeadThreshold = 9;        // seconds

        public event Action<string> OnWorkerDead;

        // Worker side — sends ping every 3 seconds
        public async Task StartSendingAsync(string masterIp, string workerId)
        {

            UdpClient udp = new UdpClient();
            byte[] data = Encoding.UTF8.GetBytes(workerId);
            while (true)
            {
                await udp.SendAsync(data, data.Length, masterIp, 9999);
                await Task.Delay(3000); // wait 3 seconds
            }
            
        }

        // Master side — listens for pings
        public async Task StartListeningAsync(List<WorkerNode> workers)
        {
            UdpClient listener = new UdpClient(9999);
            while (true)
            {
                UdpReceiveResult result = await listener.ReceiveAsync();
                string id = Encoding.UTF8.GetString(result.Buffer);
                var worker = workers.FirstOrDefault(w => w.WorkerId == id);
                if (worker != null)
                    worker.LastHeartbeat = DateTime.UtcNow;
            }
        }

        // Master side — checks for dead workers
        public async Task StartDeadWorkerCheck(List<WorkerNode> workers)
        {
            while (true)
            {
                await Task.Delay(DeadThreshold * 1000);
                foreach (var worker in workers)
                {
                    double secondsSinceLastPing =
                        (DateTime.UtcNow - worker.LastHeartbeat).TotalSeconds;
                    if (secondsSinceLastPing > DeadThreshold)
                    {
                        OnWorkerDead?.Invoke(worker.WorkerId);
                    }
                }
            }
        }
    }
}
