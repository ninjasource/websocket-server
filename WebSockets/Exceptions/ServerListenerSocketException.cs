using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Net.Sockets;

namespace WebSockets.Exceptions
{
    [Serializable]
    public class ServerListenerSocketException : Exception
    {
        public ServerListenerSocketException() : base()
        {
            
        }

        public ServerListenerSocketException(string message) : base(message)
        {
            
        }

        public ServerListenerSocketException(string message, Exception inner) : base(message, inner)
        {

        }

        public ServerListenerSocketException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }
}
