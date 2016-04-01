using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using WebSockets.Common;
using System.Diagnostics;
using System.Security.Policy;
using WebSockets.Exceptions;
using WebSockets.Server.WebSocket;
using WebSockets.Server.Http;
using System.Threading;
using WebSockets.Events;
using System.Threading.Tasks;

namespace WebSockets.Client
{
    public class WebSocketClient : WebSocketBase, IDisposable
    {
        private readonly bool _noDelay;
        private readonly IWebSocketLogger _logger;
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private Uri _uri;
        private ManualResetEvent _conectionCloseWait;

        public WebSocketClient(bool noDelay, IWebSocketLogger logger) : base(logger)
        {
            _noDelay = noDelay;
            _logger = logger;
            _conectionCloseWait = new ManualResetEvent(false);
        }

        public virtual void OpenBlocking(Uri uri)
        {
            if (!_isOpen)
            {
                string host = uri.Host;
                int port = uri.Port;
                _tcpClient = new TcpClient();
                _tcpClient.NoDelay = _noDelay;

                IPAddress ipAddress;
                if (IPAddress.TryParse(host, out ipAddress))
                {
                    _tcpClient.Connect(ipAddress, port);
                }
                else
                {
                    _tcpClient.Connect(host, port);
                }

                _networkStream = _tcpClient.GetStream();
                _uri = uri;
                _isOpen = true;
                base.OpenBlocking(_networkStream, _tcpClient.Client);
                _isOpen = false;
            }
        }

        protected override void PerformHandshake(NetworkStream networkStream)
        {
Uri uri = _uri;
WebSocketFrameReader reader = new WebSocketFrameReader();
Random rand = new Random();
byte[] keyAsBytes = new byte[16];
rand.NextBytes(keyAsBytes);
string secWebSocketKey = Convert.ToBase64String(keyAsBytes);

string handshakeHttpRequestTemplate = @"GET {0} HTTP/1.1{4}" +
                                        "Host: {1}:{2}{4}" +
                                        "Upgrade: websocket{4}" +
                                        "Connection: Upgrade{4}" +
                                        "Sec-WebSocket-Key: {3}{4}" +
                                        "Sec-WebSocket-Version: 13{4}{4}";

string handshakeHttpRequest = string.Format(handshakeHttpRequestTemplate, uri.PathAndQuery, uri.Host, uri.Port, secWebSocketKey, Environment.NewLine);
byte[] httpRequest = Encoding.UTF8.GetBytes(handshakeHttpRequest);
networkStream.Write(httpRequest, 0, httpRequest.Length);
_logger.Information(this.GetType(), "Handshake sent. Waiting for response.");

// make sure we escape the accept string which could contain special regex characters
string regexPattern = "Sec-WebSocket-Accept: (.*)";
Regex regex = new Regex(regexPattern);

            string response = string.Empty;

            try
            {
                response = HttpHelper.ReadHttpHeader(networkStream);
            }
            catch (Exception ex)
            {
                throw new WebSocketHandshakeFailedException("Handshake unexpected failure", ex);
            }

            // check the accept string
            string expectedAcceptString = base.ComputeSocketAcceptString(secWebSocketKey);
            string actualAcceptString = regex.Match(response).Groups[1].Value.Trim();
            if (expectedAcceptString != actualAcceptString)
            {
                throw new WebSocketHandshakeFailedException(string.Format("Handshake failed because the accept string from the server '{0}' was not the expected string '{1}'", expectedAcceptString, actualAcceptString));
            }
            else
            {
                _logger.Information(this.GetType(), "Handshake response received. Connection upgraded to WebSocket protocol.");
            }
        }

        public virtual void Dispose()
        {
            if (_isOpen)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    // set the close reason to GoingAway
                    BinaryReaderWriter.WriteUShort((ushort)WebSocketCloseCode.GoingAway, stream, false);

                    // send close message to server to begin the close handshake
                    Send(WebSocketOpCode.ConnectionClose, stream.ToArray());
                    _logger.Information(this.GetType(), "Sent websocket close message to server. Reason: GoingAway");
                }

                // this needs to run on a worker thread so that the read loop (in the base class) is not blocked
                Task.Factory.StartNew(WaitForServerCloseMessage);
            }
        }

        private void WaitForServerCloseMessage()
        {
            // as per the websocket spec, the server must close the connection, not the client. 
            // The client is free to close the connection after a timeout period if the server fails to do so
            _conectionCloseWait.WaitOne(TimeSpan.FromSeconds(10));

            // this will only happen if the server has failed to reply with a close response
            if (_isOpen)
            {
                _logger.Warning(this.GetType(), "Server failed to respond with a close response. Closing the connection from the client side.");

                // wait for data to be sent before we close the stream and client
                _tcpClient.Client.Shutdown(SocketShutdown.Both);
                _networkStream.Close();
                _tcpClient.Close();
            }

            _logger.Information(this.GetType(), "Client: Connection closed");
        }

        protected override void OnConnectionClose(byte[] payload)
        {
            // server has either responded to a client close request or closed the connection for its own reasons
            // the server will close the tcp connection so the client will not have to do it
            _isOpen = false;
            _conectionCloseWait.Set();
            base.OnConnectionClose(payload);
        }

    }
}
