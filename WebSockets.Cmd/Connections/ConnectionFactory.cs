using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using WebSockets.Server.Connections;
using WebSockets.Cmd.Connections;
using System.Diagnostics;

namespace WebSockets.Cmd.Connections
{
    class ConnectionFactory : IConnectionFactory
    {
        private readonly string _webRoot;

        private string GetWebRoot()
        {
            if (!string.IsNullOrWhiteSpace(_webRoot) && Directory.Exists(_webRoot))
            {
                return _webRoot;
            }

            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase).Replace(@"file:\", string.Empty);
        }

        public ConnectionFactory(string webRoot)
        {
            _webRoot = string.IsNullOrWhiteSpace(webRoot) ? GetWebRoot() : webRoot;
            Trace.TraceInformation("Web root: {0}", _webRoot);
        }

        public ConnectionFactory() : this(null)
        {
            
        }

        public IConnection CreateInstance(ConnectionDetails connectionDetails)
        {
            switch (connectionDetails.ConnectionType)
            {
                case ConnectionType.WebSocket:
                    // you can support different kinds of web socket connections using a different path
                    if (connectionDetails.Path == "/chat")
                    {
                        return new ChatWebSocketConnection(connectionDetails.NetworkStream, connectionDetails.TcpClient, connectionDetails.Header);
                    }
                    break;
                case ConnectionType.Http:
                    // this path actually refers to the reletive location of some html file or image
                    return new HttpConnection(connectionDetails.NetworkStream, connectionDetails.Path, _webRoot);
            }

            return new BadRequestConnection(connectionDetails.NetworkStream, connectionDetails.Header);
        }
    }
}
