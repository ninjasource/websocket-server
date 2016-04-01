using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSockets.Events
{
    public class PongEventArgs : EventArgs
    {
        public byte[] Payload { get; private set; }

        public PongEventArgs(byte[] payload)
        {
            Payload = payload;
        }
    }
}
