using System;
using System.Net.Sockets;

namespace EasyTcp3.ClientUtils.Internal
{
    /// <summary>
    /// Internal functions to receive messages from a remote host
    /// </summary>
    internal static class OnReceiveUtil
    {
        /// <summary>
        /// Start listening for incoming data for a specific client
        /// </summary>
        /// <param name="client"></param>
        internal static void StartListening(this EasyTcpClient client)
            => client.BaseSocket.BeginReceive(client.Buffer = new byte[client.Protocol.BufferSize], 0,
                client.Buffer.Length, SocketFlags.None, OnReceive, client);

        /// <summary>
        /// Function that gets triggered when data is received
        /// </summary>
        /// <param name="ar"></param>
        private static void OnReceive(IAsyncResult ar)
        {
            var client = ar.AsyncState as EasyTcpClient;
            if (client?.BaseSocket == null) return;

            try
            {
                int receivedBytes = client.BaseSocket.EndReceive(ar);
                client.Protocol.DataReceive(client.Buffer, receivedBytes, client);
                client.StartListening();
            }
            catch (SocketException)
            {
                client.HandleDisconnect();
            }
            catch (Exception ex)
            {
                client.BaseSocket.EndReceive(ar);
                client.FireOnError(ex);
                client.StartListening();
            }
        }

        /// <summary>
        /// Handle disconnect for a specific client
        /// </summary>
        /// <param name="client"></param>
        internal static void HandleDisconnect(this EasyTcpClient client)
        {
            client.FireOnDisconnect();
            client.Dispose();
        }
    }
}