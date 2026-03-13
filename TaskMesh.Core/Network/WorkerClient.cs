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
        public event Action<int> OnSessionStartReceived;
        private TcpClient _client;
        private NetworkStream _stream;
        private MessageSerializer _serializer = new MessageSerializer();
        private string _workerId;
        private string _masterIp;
        const int Port = 9000;
        public async Task ConnectAsync(string masterIp, string workerId, string workerName, List<Guid> existingProblemIds)
        {
            _masterIp = masterIp;
            _workerId = workerId;

            string localIp = Dns.GetHostEntry(Dns.GetHostName())
                .AddressList
                .First(a => a.AddressFamily == AddressFamily.InterNetwork)
                .ToString();
            _client?.Close();
            _client = new TcpClient();
            await _client.ConnectAsync(_masterIp, Port);
            _stream = _client.GetStream();

            RegisterRequest request = new RegisterRequest
            {
                WorkerId = _workerId,
                WorkerName = workerName,
                IpAddress = localIp,
                CurrentLoad = 0,
                WorkStatus = CurrentStatus.Available,
                ExistingProblemIds = existingProblemIds // ← add this

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
            byte[] data = _serializer.Serialize(result);
            byte[] wrapped = _serializer.WrapWithTypeAndLength("JUDGE_RESULT", data);
            await _stream.WriteAsync(wrapped, 0, wrapped.Length);
        }

        public async Task SendTabSwitchAlertAsync()
        {
            var alert = new TabSwitchAlertMessage
            {
                WorkerId = _workerId,
                AlertTime = DateTime.UtcNow
            };
            byte[] data = _serializer.Serialize(alert);
            byte[] wrapped = _serializer.WrapWithTypeAndLength("TAB_SWITCH", data);
            await _stream.WriteAsync(wrapped, 0, wrapped.Length);
        }

        public async Task ReceiveSessionStartAsync(Action<int> onSessionStart)
        {
            // This runs in ListenForProblemsAsync loop
            // Handle SESSION_START type
        }

        private async Task ListenForProblemsAsync()
        {
            while (true)
            {
                try
                {
                    byte[] headerBuf = new byte[36];
                    await _stream.ReadAsync(headerBuf, 0, 36);
                    string msgType = Encoding.UTF8.GetString(headerBuf, 0, 32).Trim();
                    int msgLength = BitConverter.ToInt32(headerBuf, 32);
                    byte[] msgBuf = new byte[msgLength];
                    await _stream.ReadAsync(msgBuf, 0, msgLength);

                    if (msgType == "PROBLEM")
                    {
                        var problem = _serializer.Deserialize<ProblemAssignment>(msgBuf);
                        OnProblemReceived?.Invoke(problem);
                    }
                    else if (msgType == "SESSION_START")
                    {
                        var session = _serializer.Deserialize<SessionStartMessage>(msgBuf);
                        OnSessionStartReceived?.Invoke(session.DurationMinutes);
                    }
                }
                catch { break; }
            }
        }
    }
}
