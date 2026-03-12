using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TaskMesh.Core.Messages;
using TaskMesh.Core.Models;

namespace TaskMesh.Core.Network
{
    public class WorkerClient
    {
        public event Action<ProblemAssignment> OnProblemReceived;
        private TcpClient _client;
        private NetworkStream _stream;
        private MessageSerializer _serializer = new MessageSerializer();
        private string _workerId;
        private string _masterIp;
        const int Port = 9000;
        public async Task ConnectAsync(string masterIp, string workerId, string workerName)
        {
            _masterIp = masterIp;
            _workerId = workerId;

            string localIp = Dns.GetHostEntry(Dns.GetHostName())
                .AddressList
                .First(a => a.AddressFamily == AddressFamily.InterNetwork)
                .ToString();

            _client = new TcpClient();
            await _client.ConnectAsync(_masterIp, Port);
            _stream = _client.GetStream();

            RegisterRequest request = new RegisterRequest
            {
                WorkerId = _workerId,
                WorkerName = workerName,
                IpAddress = localIp,
                CurrentLoad = 0,
                WorkStatus = CurrentStatus.Available
            };

            byte[] bytes = _serializer.WrapWithLength(_serializer.Serialize(request));
            await _stream.WriteAsync(bytes, 0, bytes.Length);

            byte[] lengthBuffer = new byte[4];
            await _stream.ReadAsync(lengthBuffer, 0, 4);
            int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
            byte[] messageBuffer = new byte[messageLength];
            await _stream.ReadAsync(messageBuffer, 0, messageLength);
            RegisterResponse response = _serializer.Deserialize<RegisterResponse>(messageBuffer);

            if (response.IsSuccess)
            {
                // Start listening in background — don't await
                _ = Task.Run(() => ListenForProblemsAsync());
            }
        }
        public async Task SendResultAsync(JudgeResultMessage result)
        {
            byte[] bytes = _serializer.WrapWithLength(_serializer.Serialize(result));
            await _stream.WriteAsync(bytes, 0, bytes.Length);
        }
        private async Task ListenForProblemsAsync()
        {
            while (true)
            {
                byte[] lengthBuffer = new byte[4];
                await _stream.ReadAsync(lengthBuffer, 0, 4);
                int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                byte[] messageBuffer = new byte[messageLength];
                await _stream.ReadAsync(messageBuffer, 0, messageLength);
                ProblemAssignment problem = _serializer.Deserialize<ProblemAssignment>(messageBuffer);
                OnProblemReceived?.Invoke(problem);
            }
        }
    }
}
