using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using EasyTcp4.Protocols;

namespace EasyTcp4.Encryption.Ssl
{
    /// <summary>
    /// Protocol that doesn't implements any framing
    /// Useful when communicating with an already existing tcp server/client
    /// </summary>
    public class PlainSslProtocol : SslProtocol
    {
        /// <summary>
        /// Default bufferSize when not specified
        /// </summary>
        public const int DefaultBufferSize = 1024;

        /// <summary>
        /// Size of the receive buffer 
        /// </summary>
        public sealed override int BufferSize { get; protected set; }

        /// <summary></summary>
        /// <param name="certificate">server certificate</param>
        /// <param name="bufferSize">size of the receive buffer, maximum size of a message</param>
        public PlainSslProtocol(X509Certificate certificate, int bufferSize = DefaultBufferSize) : base(certificate)
            => BufferSize = bufferSize;

        /// <summary></summary>
        /// <param name="serverName">domain name of server, must be the same as in server certificate</param>
        /// <param name="acceptInvalidCertificates">determines whether the client can connect to servers that use an invalid certificate</param>
        /// <param name="bufferSize">size of the receive buffer, maximum size of a message</param>
        public PlainSslProtocol(string serverName, bool acceptInvalidCertificates = false, int bufferSize = DefaultBufferSize)
            : base(serverName, acceptInvalidCertificates)
            => BufferSize = bufferSize;

        /// <summary>
        /// Send message to remote host
        /// </summary>
        public override void SendMessage(EasyTcpClient client, params byte[][] messageData)
        {
            if (messageData == null || messageData.Length == 0) return;
            if (client?.BaseSocket == null || !client.BaseSocket.Connected)
                throw new Exception("Could not send data: client not connected or null");

            // Calculate length of message
            var messageLength = messageData.Sum(t => t?.Length ?? 0);
            if (messageLength == 0) return;
            byte[] message = new byte[messageLength];

            // Add data to message
            int offset = 0;
            foreach (var d in messageData)
            {
                if (d == null) continue;
                Buffer.BlockCopy(d, 0, message, offset, d.Length);
                offset += d.Length;
            }

            // Send data
            client.FireOnDataSend(message);
            SslStream.Write(message, 0, message.Length);
        }

        /// <summary>
        /// Handle received data, this function should trigger the OnDataReceive event of the passed client
        /// </summary>
        /// <param name="data">received data</param>
        /// <param name="receivedBytes">amount of received bytes</param>
        /// <param name="client">client that received the data</param>
        public override async Task OnDataReceive(byte[] data, int receivedBytes, EasyTcpClient client)
        {
#if (NETCOREAPP3_1 || NET5_0 | NET6_0)
            await client.DataReceiveHandler(new Message(data[..receivedBytes], client)); // More optimized solution
#else
            byte[] receivedData = new byte[receivedBytes];
            Buffer.BlockCopy(data, 0, receivedData, 0, receivedBytes);
            await client.DataReceiveHandler(new Message(receivedData, client));
#endif
        }

        /// <summary>
        /// Return new instance of protocol 
        /// </summary>
        public override IEasyProtocol Clone()
        {
            if (Certificate != null) return new PlainSslProtocol(Certificate, BufferSize);
            else return new PlainSslProtocol(ServerName, AcceptInvalidCertificates, BufferSize);
        }
    }
}
