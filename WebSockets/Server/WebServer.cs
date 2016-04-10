using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using WebSockets.Exceptions;
using WebSockets.Server;
using WebSockets.Server.Http;
using WebSockets.Common;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace WebSockets
{
    public class WebServer : IDisposable
    {
        // maintain a list of open connections so that we can notify the client if the server shuts down
        private readonly List<IDisposable> _openConnections;
        private readonly IServiceFactory _serviceFactory;
        private readonly IWebSocketLogger _logger;
        private X509Certificate2 _sslCertificate;
        private TcpListener _listener;
        private bool _isDisposed = false;

        public WebServer(IServiceFactory serviceFactory, IWebSocketLogger logger)
        {
            _serviceFactory = serviceFactory;
            _logger = logger;
            _openConnections = new List<IDisposable>();
        }

        public void Listen(int port, X509Certificate2 sslCertificate)
        {
            try
            {
                _sslCertificate = sslCertificate;
                IPAddress localAddress = IPAddress.Any;
                _listener = new TcpListener(localAddress, port);
                _listener.Start();
                _logger.Information(this.GetType(), "Server started listening on port {0}", port);
                StartAccept();
            }
            catch (SocketException ex)
            {
                string message = string.Format("Error listening on port {0}. Make sure IIS or another application is not running and consuming your port.", port);
                throw new ServerListenerSocketException(message, ex);
            }
        }

        /// <summary>
        /// Listens on the port specified
        /// </summary>
        public void Listen(int port)
        {
            Listen(port, null);
        }

        /// <summary>
        /// Gets the first available port and listens on it. Returns the port
        /// </summary>
        public int Listen()
        {
            IPAddress localAddress = IPAddress.Any;
            _listener = new TcpListener(localAddress, 0);
            _listener.Start();
            StartAccept();
            int port = ((IPEndPoint) _listener.LocalEndpoint).Port;
            _logger.Information(this.GetType(), "Server started listening on port {0}", port);
            return port;
        }

        private void StartAccept()
        {
            // this is a non-blocking operation. It will consume a worker thread from the threadpool
            _listener.BeginAcceptTcpClient(new AsyncCallback(HandleAsyncConnection), null);
        }

        private static ConnectionDetails GetConnectionDetails(Stream stream, TcpClient tcpClient)
        {
            // read the header and check that it is a GET request
            string header = HttpHelper.ReadHttpHeader(stream);
            Regex getRegex = new Regex(@"^GET(.*)HTTP\/1\.1", RegexOptions.IgnoreCase);

            Match getRegexMatch = getRegex.Match(header);
            if (getRegexMatch.Success)
            {
                // extract the path attribute from the first line of the header
                string path = getRegexMatch.Groups[1].Value.Trim();

                // check if this is a web socket upgrade request
                Regex webSocketUpgradeRegex = new Regex("Upgrade: websocket", RegexOptions.IgnoreCase);
                Match webSocketUpgradeRegexMatch = webSocketUpgradeRegex.Match(header);

                if (webSocketUpgradeRegexMatch.Success)
                {
                    return new ConnectionDetails(stream, tcpClient, path, ConnectionType.WebSocket, header);
                }
                else
                {
                    return new ConnectionDetails(stream, tcpClient, path, ConnectionType.Http, header);
                }
            }
            else
            {
                return new ConnectionDetails(stream, tcpClient, string.Empty, ConnectionType.Unknown, header); 
            }
        }

        private Stream GetStream(TcpClient tcpClient)
        {
            Stream stream = tcpClient.GetStream();

            // we have no ssl certificate
            if (_sslCertificate == null)
            {
                _logger.Information(this.GetType(), "Connection not secure");
                return stream;
            }

            try
            {
                SslStream sslStream = new SslStream(stream, false);
                _logger.Information(this.GetType(), "Attempting to secure connection...");
                sslStream.AuthenticateAsServer(_sslCertificate, false, SslProtocols.Tls, true);
                _logger.Information(this.GetType(), "Connection successfully secured");
                return sslStream;
            }
            catch (AuthenticationException e)
            {
                // TODO: send 401 Unauthorized
                throw;
            }
        }

        private void HandleAsyncConnection(IAsyncResult res)
        {
            try
            {
                if (_isDisposed)
                {
                    return;
                }

                // this worker thread stays alive until either of the following happens:
                // Client sends a close conection request OR
                // An unhandled exception is thrown OR
                // The server is disposed
                using (TcpClient tcpClient = _listener.EndAcceptTcpClient(res))
                {
                    // we are ready to listen for more connections (on another thread)
                    StartAccept();
                    _logger.Information(this.GetType(), "Server: Connection opened");

                    // get a secure or insecure stream
                    Stream stream = GetStream(tcpClient);

                    // extract the connection details and use those details to build a connection
                    ConnectionDetails connectionDetails = GetConnectionDetails(stream, tcpClient);
                    using (IService service = _serviceFactory.CreateInstance(connectionDetails))
                    {
                        try
                        {
                            // record the connection so we can close it if something goes wrong
                            lock (_openConnections)
                            {
                                _openConnections.Add(service);
                            }

                            // respond to the http request.
                            // Take a look at the WebSocketConnection or HttpConnection classes
                            service.Respond();
                        }
                        finally
                        {
                            // forget the connection, we are done with it
                            lock (_openConnections)
                            {
                                _openConnections.Remove(service);
                            }
                        }
                    }
                }

                _logger.Information(this.GetType(), "Server: Connection closed");
            }
            catch (ObjectDisposedException)
            {
                // do nothing. This will be thrown if the Listener has been stopped
            }
            catch (Exception ex)
            {
                _logger.Error(this.GetType(), ex);
            }
        }

        private void CloseAllConnections()
        {
            IDisposable[] openConnections;

            lock (_openConnections)
            {
                openConnections = _openConnections.ToArray();
                _openConnections.Clear();
            }

            // safely attempt to close each connection
            foreach (IDisposable openConnection in openConnections)
            {
                try
                {
                    openConnection.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.Error(this.GetType(), ex);
                }
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                // safely attempt to shut down the listener
                try
                {
                    if (_listener != null)
                    {
                        if (_listener.Server != null)
                        {
                            _listener.Server.Close();
                        }

                        _listener.Stop();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(this.GetType(), ex);
                }

                CloseAllConnections();
                _logger.Information(this.GetType(), "Web Server disposed");
            }
        }
    }
}
