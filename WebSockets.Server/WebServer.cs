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
using WebSockets.Server.Connections;

namespace WebSockets.Server
{
    public class WebServer : IDisposable
    {
        // maintain a list of open connections so that we can notify the client if the server shuts down
        private readonly List<IDisposable> _openConnections;
        private readonly IConnectionFactory _connectionFactory;
        private TcpListener _listener;
        private bool _isDisposed = false;

        public WebServer(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            _openConnections = new List<IDisposable>();
        }

        /// <summary>
        /// Listens on the port specified
        /// </summary>
        public void Listen(int port)
        {
            try
            {
                IPAddress localAddress = IPAddress.Any;
                _listener = new TcpListener(localAddress, port);
                _listener.Start();
                Trace.WriteLine("Server started listening on port " + port);
                StartAccept();
            }
            catch (SocketException ex)
            {
                Trace.TraceError(string.Format("Error listening on port {0}. Make sure IIS or another application is not running and consuming your port.{1}{2}", port, Environment.NewLine, ex.ToString()));
            }
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
            Trace.WriteLine("Server started listening on port " + port);
            return port;
        }

        private void StartAccept()
        {
            // this is a non-blocking operation. It will consume a worker thread from the threadpool
            _listener.BeginAcceptTcpClient(new AsyncCallback(HandleAsyncConnection), null);
        }

        private static ConnectionDetails GetConnectionDetails(NetworkStream networkStream, TcpClient tcpClient)
        {
            // read the header and check that it is a GET request
            string header = HttpHelper.ReadHttpHeader(networkStream);
            Regex getRegex = new Regex(@"^GET(.*)HTTP\/1\.1");

            Match getRegexMatch = getRegex.Match(header);
            if (getRegexMatch.Success)
            {
                // extract the path attribute from the first line of the header
                string path = getRegexMatch.Groups[1].Value.Trim();

                // check if this is a web socket upgrade request
                Regex webSocketUpgradeRegex = new Regex("Upgrade: websocket");
                Match webSocketUpgradeRegexMatch = webSocketUpgradeRegex.Match(header);

                if (webSocketUpgradeRegexMatch.Success)
                {
                    return new ConnectionDetails(networkStream, tcpClient, path, ConnectionType.WebSocket, header);
                }
                else
                {
                    return new ConnectionDetails(networkStream, tcpClient, path, ConnectionType.Http, header);
                }
            }
            else
            {
                return new ConnectionDetails(networkStream, tcpClient, string.Empty, ConnectionType.Unknown, header); 
            }
        }

        private void HandleAsyncConnection(IAsyncResult res)
        {
            try
            {
                // this worker thread stays alive until either of the following happens:
                // Client sends a close conection request OR
                // An unhandled exception is thrown OR
                // The server is disposed
                using (TcpClient tcpClient = _listener.EndAcceptTcpClient(res))
                {
                    // we are ready to listen for more connections (on another thread)
                    StartAccept();
                    Trace.WriteLine("Connection opened");
                    
                    using (NetworkStream networkStream = tcpClient.GetStream())
                    {
                        // extract the connection details and use those details to build a connection
                        ConnectionDetails connectionDetails = GetConnectionDetails(networkStream, tcpClient);
                        using (IConnection connection = _connectionFactory.CreateInstance(connectionDetails))
                        {
                            try
                            {
                                // record the connection so we can close it if something goes wrong
                                lock (_openConnections)
                                {
                                    _openConnections.Add(connection);
                                }

                                // respong to the http request.
                                // Take a look at the WebSocketConnection or HttpConnection classes
                                connection.Respond();
                            }
                            finally
                            {
                                // forget the connection, we are done with it
                                lock (_openConnections)
                                {
                                    _openConnections.Remove(connection);
                                }
                            }
                        }
                    }

                    Trace.WriteLine("Connection closed");
                }
            }
            catch (ObjectDisposedException)
            {
                // do nothing. This will be thrown if the Listener has been stopped
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        private void CloseAllConnections()
        {
            lock (_openConnections)
            {
                // safely attempt to close each connection
                foreach (IDisposable openConnection in _openConnections)
                {
                    try
                    {
                        openConnection.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(ex.ToString());
                    }
                }

                _openConnections.Clear();
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                // safely attempt to shut down the listener
                try
                {
                    _listener.Server.Close();
                    _listener.Stop();
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }

                CloseAllConnections();
                _isDisposed = true;
                Trace.WriteLine("Web Server disposed");
            }
        }
    }
}
