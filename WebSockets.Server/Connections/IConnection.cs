using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSockets.Server.Connections
{
    public interface IConnection : IDisposable
    {
        /// <summary>
        /// Sends data back to the client. This is built using the IConnectionFactory
        /// </summary>
        void Respond();
    }
}
