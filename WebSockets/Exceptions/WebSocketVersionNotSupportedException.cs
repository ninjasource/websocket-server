using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace WebSockets.Exceptions
{
    [Serializable]
    public class WebSocketVersionNotSupportedException : Exception
    {
        public WebSocketVersionNotSupportedException() : base()
        {
            
        }

        public WebSocketVersionNotSupportedException(string message) : base(message)
        {
            
        }

        public WebSocketVersionNotSupportedException(string message, Exception inner) : base(message, inner)
        {

        }

        public WebSocketVersionNotSupportedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }
}
