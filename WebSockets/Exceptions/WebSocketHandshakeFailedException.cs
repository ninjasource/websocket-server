using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace WebSockets.Exceptions
{
    [Serializable]
    public class WebSocketHandshakeFailedException : Exception
    {
        public WebSocketHandshakeFailedException() : base()
        {
            
        }

        public WebSocketHandshakeFailedException(string message) : base(message)
        {
            
        }

        public WebSocketHandshakeFailedException(string message, Exception inner) : base(message, inner)
        {

        }

        public WebSocketHandshakeFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }
}
