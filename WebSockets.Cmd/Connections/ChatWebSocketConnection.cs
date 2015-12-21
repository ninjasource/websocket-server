using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Net.Sockets;
using WebSockets.Server.WebSocketProtocol;

namespace WebSockets.Cmd.Connections
{
    class ChatWebSocketConnection : WebSocketConnection
    {
        public ChatWebSocketConnection(NetworkStream networkStream, TcpClient tcpClient, string header)
            : base(networkStream, header)
        {
            // send requests immediately if true (needed for small low latency packets but not a long stream). 
            // Basically, dont wait for buffer to be full
            tcpClient.NoDelay = true;
        }

        protected override void OnTextFrame(string text)
        {
            string response = "Server: " + text;
            base.Writer.WriteText(response);
            Trace.TraceInformation(response);
        }
    }
}
