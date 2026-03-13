using System;
using System.Collections.Generic;
using System.IO;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TaskMesh.Core.Messages;
using TaskMesh.Core.Models;

namespace TaskMesh.Core.Network
{
    public class MasterServer
    {
        public event Action<JudgeResultMessage> OnResultReceived;
        public event Action<string, string, string, List<Guid>> OnWorkerRegistered;
        private Dictionary<string, NetworkStream> _workerStreams
    = new Dictionary<string, NetworkStream>();
        const int port = 9000; // example port
        private TcpListener _listener;
        private List<WorkerNode> _connectedWorkers = new List<WorkerNode>();
        private MessageSerializer _serializer = new MessageSerializer();
        public async Task StartListening()
        {

            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            while (true)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync(); // background wait
                _ = Task.Run(() => HandleWorkerAsync(client));
            }

        }
        public async Task HandleWorkerAsync(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] lengthBuffer = new byte[4];
            await stream.ReadAsync(lengthBuffer, 0, 4);
            int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
            byte[] messageBuffer = new byte[messageLength];
            await stream.ReadAsync(messageBuffer, 0, messageLength);
            RegisterRequest request = _serializer.Deserialize<RegisterRequest>(messageBuffer);
            var existingWorker = _connectedWorkers.FirstOrDefault(w => w.WorkerId == request.WorkerId);

            if (existingWorker == null)
            {
                // New worker — create and add
                WorkerNode newWorker = new WorkerNode
                {
                    WorkerId = request.WorkerId,
                    IpAddress = request.IpAddress,
                    CurrentLoad = request.CurrentLoad,
                    WorkStatus = request.WorkStatus
                };
                _connectedWorkers.Add(newWorker);
            }
            else
            {
                // Existing worker — update
                existingWorker.IpAddress = request.IpAddress;
                existingWorker.WorkStatus = request.WorkStatus;
                existingWorker.LastHeartbeat = DateTime.UtcNow;
            }
            _workerStreams[request.WorkerId] = stream;
            OnWorkerRegistered?.Invoke(
    request.WorkerId,
    request.IpAddress,
    request.WorkerName,
    request.ExistingProblemIds);
            RegisterResponse response = new RegisterResponse
            {
                IsSuccess = true,
                WelcomeMessage = $"Welcome {request.WorkerId}"
            };
            byte[] responseBytes = _serializer.Serialize(response);
            byte[] wrappedBytes = _serializer.WrapWithLength(responseBytes);
            await stream.WriteAsync(wrappedBytes, 0, wrappedBytes.Length);
                while (true)
                {
                    try
                    {
                        byte[] lengthBuf = new byte[4];
                        int read = await stream.ReadAsync(lengthBuf, 0, 4);
                        if (read == 0) break; // connection closed
                        int msgLength = BitConverter.ToInt32(lengthBuf, 0);
                        byte[] msgBuf = new byte[msgLength];
                        await stream.ReadAsync(msgBuf, 0, msgLength);
                        JudgeResultMessage result = _serializer.Deserialize<JudgeResultMessage>(msgBuf);
                        OnResultReceived?.Invoke(result);
                    }
                    catch
                    {
                        break; // error or connection closed
                    }
                }
                _workerStreams.Remove(request.WorkerId);
                _connectedWorkers.RemoveAll(w => w.WorkerId == request.WorkerId);
        }
        public NetworkStream GetWorkerStream(string workerId)
        {
            _workerStreams.TryGetValue(workerId, out var stream);
            return stream;
        }

    }
}
