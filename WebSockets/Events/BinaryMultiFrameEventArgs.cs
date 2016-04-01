using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSockets.Events
{
    public class BinaryMultiFrameEventArgs : BinaryFrameEventArgs
    {
        public bool IsLastFrame { get; private set; }

        public BinaryMultiFrameEventArgs(byte[] payload, bool isLastFrame) : base(payload)
        {
            IsLastFrame = isLastFrame;
        }
    }
}
