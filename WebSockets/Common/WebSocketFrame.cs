using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSockets.Common
{
    public class WebSocketFrame
    {
        public bool IsFinBitSet { get; private set; }
        public WebSocketOpCode OpCode { get; private set; }
        public byte[] DecodedPayload { get; private set; }
        public bool IsValid { get; private set; }

        public WebSocketFrame(bool isFinBitSet, WebSocketOpCode webSocketOpCode, byte[] decodedPayload, bool isValid)
        {
            IsFinBitSet = isFinBitSet;
            OpCode = webSocketOpCode;
            DecodedPayload = decodedPayload;
            IsValid = isValid;
        }
    }
}
