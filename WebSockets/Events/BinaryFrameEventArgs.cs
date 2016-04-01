using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSockets.Events
{
    public class BinaryFrameEventArgs : EventArgs
    {
        public byte[] Payload { get; private set; }

        public BinaryFrameEventArgs(byte[] payload)
        {
            Payload = payload;
        }
    }
}
