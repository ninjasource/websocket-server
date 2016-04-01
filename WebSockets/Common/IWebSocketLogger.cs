using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSockets.Common
{
    public interface IWebSocketLogger
    {
        void Information(Type type, string format, params object[] args);
        void Warning(Type type, string format, params object[] args);
        void Error(Type type, string format, params object[] args);
        void Error(Type type, Exception exception);
    }
}
