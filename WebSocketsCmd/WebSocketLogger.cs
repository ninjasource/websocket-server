using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebSockets.Common;
using System.Diagnostics;

namespace WebSocketsCmd
{
    internal class WebSocketLogger : IWebSocketLogger
    {
        public void Information(Type type, string format, params object[] args)
        {
            Trace.TraceInformation(format, args);
        }

        public void Warning(Type type, string format, params object[] args)
        {
            Trace.TraceWarning(format, args);
        }

        public void Error(Type type, string format, params object[] args)
        {
            Trace.TraceError(format, args);
        }

        public void Error(Type type, Exception exception)
        {
            Error(type, "{0}", exception);
        }
    }
}
