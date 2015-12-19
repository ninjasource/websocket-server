using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSockets.Server.Exceptions
{
    public class EntityTooLargeException : Exception
    {
        /// <summary>
        /// Http header too large to fit in buffer
        /// </summary>
        public EntityTooLargeException(string message)
            : base(message)
        {
            
        }
    }
}
