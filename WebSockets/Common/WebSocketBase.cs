using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using WebSockets.Events;

namespace WebSockets.Common
{
    public abstract class WebSocketBase
    {
        private readonly IWebSocketLogger _logger;
        private readonly object _sendLocker;
        private Stream _stream;
        private WebSocketFrameWriter _writer;
        private WebSocketOpCode _multiFrameOpcode;
        private Socket _socket;
        protected bool _isOpen;

        public event EventHandler ConnectionOpened;
        public event EventHandler<ConnectionCloseEventArgs> ConnectionClose;
        public event EventHandler<PingEventArgs> Ping;
        public event EventHandler<PingEventArgs> Pong;
        public event EventHandler<TextFrameEventArgs> TextFrame;
        public event EventHandler<TextMultiFrameEventArgs> TextMultiFrame;
        public event EventHandler<BinaryFrameEventArgs> BinaryFrame;
        public event EventHandler<BinaryMultiFrameEventArgs> BinaryMultiFrame;

        public WebSocketBase(IWebSocketLogger logger)
        {
            _logger = logger;
            _sendLocker = new object();
            _isOpen = false;
        }

        protected void OpenBlocking(Stream stream, Socket socket)
        {
            _socket = socket;
            _stream = stream;
            _writer = new WebSocketFrameWriter(stream);
            PerformHandshake(stream);
            _isOpen = true;
            MainReadLoop();
        }

        protected virtual void Send(WebSocketOpCode opCode, byte[] toSend, bool isLastFrame)
        {
            if (_isOpen)
            {
                lock (_sendLocker)
                {
                    if (_isOpen)
                    {
                        _writer.Write(opCode, toSend, isLastFrame);
                    }
                }
            }
        }

        protected virtual void Send(WebSocketOpCode opCode, byte[] toSend)
        {
            Send(opCode, toSend, true);
        }

        protected virtual void Send(byte[] toSend)
        {
            Send(WebSocketOpCode.BinaryFrame, toSend, true);
        }

        protected virtual void Send(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            Send(WebSocketOpCode.TextFrame, bytes, true);
        }

        protected virtual void OnConnectionOpened()
        {
            if (ConnectionOpened != null)
            {
                ConnectionOpened(this, new EventArgs());
            }
        }

        protected virtual void OnPing(byte[] payload)
        {
            Send(WebSocketOpCode.Pong, payload);

            if (Ping != null)
            {
                Ping(this, new PingEventArgs(payload));
            }
        }

        protected virtual void OnPong(byte[] payload)
        {
            if (Pong != null)
            {
                Pong(this, new PingEventArgs(payload));
            }
        }

        protected virtual void OnTextFrame(string text)
        {
            if (TextFrame != null)
            {
                TextFrame(this, new TextFrameEventArgs(text));
            }
        }

        protected virtual void OnTextMultiFrame(string text, bool isLastFrame)
        {
            if (TextMultiFrame != null)
            {
                TextMultiFrame(this, new TextMultiFrameEventArgs(text, isLastFrame));
            }
        }

        protected virtual void OnBinaryFrame(byte[] payload)
        {
            if (BinaryFrame != null)
            {
                BinaryFrame(this, new BinaryFrameEventArgs(payload));
            }
        }

        protected virtual void OnBinaryMultiFrame(byte[] payload, bool isLastFrame)
        {
            if (BinaryMultiFrame != null)
            {
                BinaryMultiFrame(this, new BinaryMultiFrameEventArgs(payload, isLastFrame));
            }
        }

        protected virtual void OnConnectionClose(byte[] payload)
        {
            ConnectionCloseEventArgs args = GetConnectionCloseEventArgsFromPayload(payload);

            if (args.Reason == null)
            {
                _logger.Information(this.GetType(), "Received web socket close message: {0}", args.Code);
            }
            else
            {
                _logger.Information(this.GetType(), "Received web socket close message: Code '{0}' Reason '{1}'", args.Code, args.Reason);
            }

            if (ConnectionClose != null)
            {
                ConnectionClose(this, args);
            }
        }

        protected abstract void PerformHandshake(Stream stream);

        /// <summary>
        /// Combines the key supplied by the client with a guid and returns the sha1 hash of the combination
        /// </summary>
        protected string ComputeSocketAcceptString(string secWebSocketKey)
        {
            // this is a guid as per the web socket spec
            const string webSocketGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

            string concatenated = secWebSocketKey + webSocketGuid;
            byte[] concatenatedAsBytes = Encoding.UTF8.GetBytes(concatenated);
            byte[] sha1Hash = SHA1.Create().ComputeHash(concatenatedAsBytes);
            string secWebSocketAccept = Convert.ToBase64String(sha1Hash);
            return secWebSocketAccept;
        }

        protected ConnectionCloseEventArgs GetConnectionCloseEventArgsFromPayload(byte[] payload)
        {
            if (payload.Length >= 2)
            {
                using (MemoryStream stream = new MemoryStream(payload))
                {
                    ushort code = BinaryReaderWriter.ReadUShortExactly(stream, false);

                    try
                    {
                        WebSocketCloseCode closeCode = (WebSocketCloseCode)code;

                        if (payload.Length > 2)
                        {
                            string reason = Encoding.UTF8.GetString(payload, 2, payload.Length - 2);
                            return new ConnectionCloseEventArgs(closeCode, reason);
                        }
                        else
                        {
                            return new ConnectionCloseEventArgs(closeCode, null);
                        }
                    }
                    catch (InvalidCastException)
                    {
                        _logger.Warning(this.GetType(), "Close code {0} not recognised", code);
                        return new ConnectionCloseEventArgs(WebSocketCloseCode.Normal, null);
                    }
                }
            }

            return new ConnectionCloseEventArgs(WebSocketCloseCode.Normal, null);
        }

        private void MainReadLoop()
        {
            Stream stream = _stream;
            OnConnectionOpened();
            WebSocketFrameReader reader = new WebSocketFrameReader();
            List<WebSocketFrame> fragmentedFrames = new List<WebSocketFrame>();

            while (true)
            {
                WebSocketFrame frame;

                try
                {
                    frame = reader.Read(stream, _socket);
                    if (frame == null)
                    {
                        return;
                    }
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                
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
                            OnConnectionClose(frame.DecodedPayload);
                            return;
                        case WebSocketOpCode.Ping:
                            OnPing(frame.DecodedPayload);
                            break;
                        case WebSocketOpCode.Pong:
                            OnPong(frame.DecodedPayload);
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
    }
}
