using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TaskMesh.Core.Messages;

namespace TaskMesh.Core.Network
{
    public class MasterPeerServer
    {
        private TcpListener _listener;
        private MessageSerializer _serializer = new MessageSerializer();
        private List<NetworkStream> _peerStreams = new();
        private string _masterId;
        const int PeerPort = 9001;

        public event Action<PeerSyncMessage> OnSyncReceived;

        public MasterPeerServer(string masterId)
        {
            _masterId = masterId;
        }

        // Listen for incoming peer connections
        public async Task StartListeningAsync()
        {
            _listener = new TcpListener(IPAddress.Any, PeerPort);
            _listener.Start();
            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandlePeerAsync(client));
            }
        }

        // Connect to a peer master
        public async Task ConnectToPeerAsync(string peerIp)
        {
            try
            {
                var client = new TcpClient();
                await client.ConnectAsync(peerIp, PeerPort);
                var stream = client.GetStream();
                _peerStreams.Add(stream);
                _ = Task.Run(() => ListenToPeerAsync(stream));
            }
            catch
            {
                // Peer not available yet — retry logic can be added
            }
        }

        // Broadcast sync message to all peers
        public async Task BroadcastAsync(PeerSyncMessage msg)
        {
            byte[] data = _serializer.Serialize(msg);
            byte[] wrapped = _serializer.WrapWithTypeAndLength("PEER_SYNC", data);

            var deadStreams = new List<NetworkStream>();
            foreach (var stream in _peerStreams)
            {
                try
                {
                    await stream.WriteAsync(wrapped, 0, wrapped.Length);
                }
                catch
                {
                    deadStreams.Add(stream); // peer disconnected
                }
            }
            deadStreams.ForEach(s => _peerStreams.Remove(s));
        }

        private async Task HandlePeerAsync(TcpClient client)
        {
            var stream = client.GetStream();
            _peerStreams.Add(stream);
            await ListenToPeerAsync(stream);
        }

        private async Task ListenToPeerAsync(NetworkStream stream)
        {
            while (true)
            {
                try
                {
                    byte[] headerBuf = new byte[36];
                    int read = await stream.ReadAsync(headerBuf, 0, 36);
                    if (read == 0) break;

                    string msgType = Encoding.UTF8
                        .GetString(headerBuf, 0, 32).Trim();
                    int msgLength = BitConverter.ToInt32(headerBuf, 32);
                    byte[] msgBuf = new byte[msgLength];
                    await stream.ReadAsync(msgBuf, 0, msgLength);

                    if (msgType == "PEER_SYNC")
                    {
                        var msg = _serializer
                            .Deserialize<PeerSyncMessage>(msgBuf);
                        OnSyncReceived?.Invoke(msg);
                    }
                }
                catch { break; }
            }
            _peerStreams.Remove(stream);
        }
    }
}
