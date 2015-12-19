using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSockets.Server.Exceptions
{
    public class WebSocketVersionNotSupportedException : Exception
    {
        public WebSocketVersionNotSupportedException(string message) : base(message)
        {
            
        }
    }
}
