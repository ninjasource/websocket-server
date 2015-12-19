using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;
using WebSockets.Server.Exceptions;
using WebSockets.Server.Connections;

namespace WebSockets.Server.WebSocketProtocol
{
    public abstract class WebSocketConnection : IConnection
    {
        private readonly NetworkStream _networkStream;
        private readonly string _header;
        private readonly WebSocketFrameWriter _writer;

        private bool _isDisposed = false;
        private WebSocketOpCode _multiFrameOpcode;

        public WebSocketConnection(NetworkStream networkStream, string header)
        {
            _networkStream = networkStream;
            _header = header;
            _writer = new WebSocketFrameWriter(networkStream);
        }

        public void Respond()
        {
            PerformHandshake();
            MainReadLoop();
        }

        protected WebSocketFrameWriter Writer
        {
            get { return _writer; }
        }

        protected virtual void OnPing(byte[] payload)
        {
            _writer.Write(WebSocketOpCode.Pong, payload);
        }

        protected virtual void OnConnectionClosed(byte[] payload)
        {
            _writer.Write(WebSocketOpCode.ConnectionClose, payload);
            Trace.WriteLine("Client requested connection close");
        }

        protected virtual void OnTextFrame(string text)
        {
            // optional to override
        }

        protected virtual void OnTextMultiFrame(string text, bool isLastFrame)
        {
            // optional to override
        }

        protected virtual void OnBinaryFrame(byte[] payload)
        {
            // optional to override
        }

        protected virtual void OnBinaryMultiFrame(byte[] payload, bool isLastFrame)
        {
            // optional to override
        }
        
        private void PerformHandshake()
        {
            NetworkStream networkStream = _networkStream;
            string header = _header;

            try
            {
                Regex webSocketKeyRegex = new Regex("Sec-WebSocket-Key: (.*)");
                Regex webSocketVersionRegex = new Regex("Sec-WebSocket-Version: (.*)");

                // check the version. Support version 13 and above
                const int WebSocketVersion = 13;
                int secWebSocketVersion = Convert.ToInt32(webSocketVersionRegex.Match(header).Groups[1].Value.Trim());
                if (secWebSocketVersion < WebSocketVersion)
                {
                    throw new WebSocketVersionNotSupportedException(string.Format("WebSocket Version {0} not suported. Must be {1} or above", secWebSocketVersion, WebSocketVersion));
                }

                string secWebSocketKey = webSocketKeyRegex.Match(header).Groups[1].Value.Trim();
                string setWebSocketAccept = WebSocketHandshakeHelper.ComputeSocketAcceptString(secWebSocketKey);
                string response = ("HTTP/1.1 101 Switching Protocols" + Environment.NewLine
                                           + "Connection: Upgrade" + Environment.NewLine
                                           + "Upgrade: websocket" + Environment.NewLine
                                           + "Sec-WebSocket-Accept: " + setWebSocketAccept);

                HttpHelper.WriteHttpHeader(response, networkStream);
                Trace.WriteLine("Web Socket handshake sent");
            }
            catch (WebSocketVersionNotSupportedException ex)
            {
                string response = "HTTP/1.1 426 Upgrade Required" + Environment.NewLine + "Sec-WebSocket-Version: 13";
                HttpHelper.WriteHttpHeader(response, networkStream);
                throw;
            }
            catch (Exception ex)
            {
                HttpHelper.WriteHttpHeader("HTTP/1.1 400 Bad Request", networkStream);
                throw;
            }
        }

        private void MainReadLoop()
        {
            NetworkStream networkStream = _networkStream;
            WebSocketFrameReader reader = new WebSocketFrameReader();
            List<WebSocketFrame> fragmentedFrames = new List<WebSocketFrame>();

            while (true)
            {
                WebSocketFrame frame = reader.Read(networkStream);

                // if we have received unexpected data
                if (!frame.IsValid)
                {
                    return;
                }

                if (frame.OpCode == WebSocketOpCode.ContinuationFrame)
                {
                    switch (_multiFrameOpcode)
                    {
                        case WebSocketOpCode.TextFrame:
                            String data = Encoding.UTF8.GetString(frame.DecodedPayload, 0, frame.DecodedPayload.Length);
                            OnTextMultiFrame(data, frame.IsFinBitSet);
                            break;
                        case WebSocketOpCode.BinaryFrame:
                            OnBinaryMultiFrame(frame.DecodedPayload, frame.IsFinBitSet);
                            break;
                    }
                }
                else
                {
                    switch (frame.OpCode)
                    {
                        case WebSocketOpCode.ConnectionClose:
                            OnConnectionClosed(frame.DecodedPayload);
                            return;
                        case WebSocketOpCode.Ping:
                            OnPing(frame.DecodedPayload);
                            break;
                        case WebSocketOpCode.TextFrame:
                            String data = Encoding.UTF8.GetString(frame.DecodedPayload, 0, frame.DecodedPayload.Length);
                            if (frame.IsFinBitSet)
                            {
                                OnTextFrame(data);
                            }
                            else
                            {
                                _multiFrameOpcode = frame.OpCode;
                                OnTextMultiFrame(data, frame.IsFinBitSet);
                            }
                            break;
                        case WebSocketOpCode.BinaryFrame:
                            if (frame.IsFinBitSet)
                            {
                                OnBinaryFrame(frame.DecodedPayload);
                            }
                            else
                            {
                                _multiFrameOpcode = frame.OpCode;
                                OnBinaryMultiFrame(frame.DecodedPayload, frame.IsFinBitSet);
                            }
                            break;
                    }
                }
            }
        }

        public void Dispose()
        {
            // send special web socket close message. Don't close the network stream, it will be disposed later
            if (_networkStream.CanWrite && !_isDisposed)
            {
                _isDisposed = true;
                _writer.Write(WebSocketOpCode.ConnectionClose, new byte[0]);
                Trace.WriteLine("Server requested connection close");
            }
        }
    }
}
