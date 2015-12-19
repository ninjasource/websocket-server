using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using WebSockets.Server.Connections;
using WebSockets.Cmd.Connections;

namespace WebSockets.Cmd.Connections
{
    class ConnectionFactory : IConnectionFactory
    {
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
                    return new HttpConnection(connectionDetails.NetworkStream, connectionDetails.Path);
            }

            return new BadRequestConnection(connectionDetails.NetworkStream, connectionDetails.Header);
        }
    }
}
