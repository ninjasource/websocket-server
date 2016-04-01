using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSockets.Events
{
    public class TextMultiFrameEventArgs : TextFrameEventArgs
    {
        public bool IsLastFrame { get; private set; }

        public TextMultiFrameEventArgs(string text, bool isLastFrame) : base(text)
        {
            IsLastFrame = isLastFrame;
        }
    }
}
