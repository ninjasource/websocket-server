using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace WebSockets.Server.Connections
{
    public class BadRequestConnection : IConnection
    {
        private readonly NetworkStream _networkStream;
        private readonly string _header;

        public BadRequestConnection(NetworkStream networkStream, string header)
        {
            _networkStream = networkStream;
            _header = header;
        }

        public void Respond()
        {
            HttpHelper.WriteHttpHeader("HTTP/1.1 400 Bad Request", _networkStream);

            // limit what we log. Headers can be up to 16K in size
            string header = _header.Length > 255 ? _header.Substring(0,255) + "..." : _header;
            Trace.TraceInformation("Bad request: '" + header + "'");
        }

        public void Dispose()
        {
            // do nothing
        }
    }
}
