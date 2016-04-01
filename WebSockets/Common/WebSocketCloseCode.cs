using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSockets.Common
{
    public enum WebSocketCloseCode
    {
        Normal = 1000,
        GoingAway = 1001,
        ProtocolError = 1002,
        DataTypeNotSupported = 1003,
        Reserverd = 1004,
        ReserverdNoStatusCode = 1005,
        ReserverdAbnormalClosure = 1006,
        MismatchDataNonUTF8 = 1007,
        ViolationOfPolicy = 1008,
        MessageTooLarge = 1009,
        EnpointExpectsExtension = 1010,
        ServerUnexpectedCondition = 1011,
        ServerRegectTlsHandshake = 1015,
    }
}
