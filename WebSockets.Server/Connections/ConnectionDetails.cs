using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace WebSockets.Server.Connections
{
    public class ConnectionDetails
    {
        public NetworkStream NetworkStream { get; private set; }
        public TcpClient TcpClient { get; private set; }
        public ConnectionType ConnectionType { get; private set; }
        public string Header { get; private set; }

        // this is the path attribute in the first line of the http header
        public string Path { get; private set; }

        public ConnectionDetails (NetworkStream networkStream, TcpClient tcpClient, string path, ConnectionType connectionType, string header)
        {
            NetworkStream = networkStream;
            TcpClient = tcpClient;
            Path = path;
            ConnectionType = connectionType;
            Header = header;
        }
    }
}
