using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSockets.Events
{
    public class PingEventArgs : EventArgs
    {
        public byte[] Payload { get; private set; }

        public PingEventArgs(byte[] payload)
        {
            Payload = payload;
        }
    }
}
