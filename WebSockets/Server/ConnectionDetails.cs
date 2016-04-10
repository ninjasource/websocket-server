using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;

namespace WebSockets.Server
{
    public class ConnectionDetails
    {
        public Stream Stream { get; private set; }
        public TcpClient TcpClient { get; private set; }
        public ConnectionType ConnectionType { get; private set; }
        public string Header { get; private set; }

        // this is the path attribute in the first line of the http header
        public string Path { get; private set; }

        public ConnectionDetails (Stream stream, TcpClient tcpClient, string path, ConnectionType connectionType, string header)
        {
            Stream = stream;
            TcpClient = tcpClient;
            Path = path;
            ConnectionType = connectionType;
            Header = header;
        }
    }
}
