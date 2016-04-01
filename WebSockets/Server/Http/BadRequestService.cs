using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using WebSockets.Common;

namespace WebSockets.Server.Http
{
    public class BadRequestService : IService
    {
        private readonly NetworkStream _networkStream;
        private readonly string _header;
        private readonly IWebSocketLogger _logger;

        public BadRequestService(NetworkStream networkStream, string header, IWebSocketLogger logger)
        {
            _networkStream = networkStream;
            _header = header;
            _logger = logger;
        }

        public void Respond()
        {
            HttpHelper.WriteHttpHeader("HTTP/1.1 400 Bad Request", _networkStream);

            // limit what we log. Headers can be up to 16K in size
            string header = _header.Length > 255 ? _header.Substring(0,255) + "..." : _header;
            _logger.Warning(this.GetType(), "Bad request: '{0}'", header);
        }

        public void Dispose()
        {
            // do nothing
        }
    }
}
