using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
namespace TaskMesh.Core.Network
{
    public class MessageSerializer
    {
        public byte[] Serialize<T>(T message)
        {
            string jsonstring = JsonSerializer.Serialize(message);
            return Encoding.UTF8.GetBytes(jsonstring);
        }
        public T Deserialize<T>(byte[] data)
        {
            string jsonstring = Encoding.UTF8.GetString(data);
            return JsonSerializer.Deserialize<T>(jsonstring);
        }
        public byte[] WrapWithLength(byte[] data)
        {

            byte[] lengthBytes = BitConverter.GetBytes(data.Length);
            byte[] result = new byte[4 + data.Length];
            Buffer.BlockCopy(lengthBytes, 0, result, 0, 4);
            Buffer.BlockCopy(data, 0, result, 4, data.Length);
            return result;
        }
        public byte[] UnwrapWithLength(byte[] data)
        {
            int length = BitConverter.ToInt32(data, 0);
            byte[] messageBytes = new byte[length];
            Buffer.BlockCopy(data, 4, messageBytes, 0, length);
            return messageBytes;
        }
        public byte[] WrapWithTypeAndLength(string messageType, byte[] data)
        {
            byte[] typeBytes = Encoding.UTF8.GetBytes(messageType.PadRight(32));
            byte[] lengthBytes = BitConverter.GetBytes(data.Length);
            byte[] result = new byte[36 + data.Length];
            Buffer.BlockCopy(typeBytes, 0, result, 0, 32);
            Buffer.BlockCopy(lengthBytes, 0, result, 32, 4);
            Buffer.BlockCopy(data, 0, result, 36, data.Length);
            return result;
        }

        public (string messageType, byte[] data) UnwrapWithTypeAndLength(byte[] raw)
        {
            string messageType = Encoding.UTF8.GetString(raw, 0, 32).Trim();
            int length = BitConverter.ToInt32(raw, 32);
            byte[] data = new byte[length];
            Buffer.BlockCopy(raw, 36, data, 0, length);
            return (messageType, data);
        }
    }
}
