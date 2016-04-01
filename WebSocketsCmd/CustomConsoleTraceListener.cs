using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace WebSocketsCmd
{
    public class CustomConsoleTraceListener : TraceListener
    {
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            string message = string.Format(format, args);

            // write the localised date and time but include the time zone in brackets (good for combining logs from different timezones)
            TimeSpan utcOffset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
            string plusOrMinus = (utcOffset < TimeSpan.Zero) ? "-" : "+";
            string utcHourOffset = utcOffset.TotalHours == 0 ? string.Empty : string.Format(" ({0}{1:hh})", plusOrMinus, utcOffset);
            string dateWithOffset = string.Format(@"{0:yyyy/MM/dd HH:mm:ss.fff}{1}", DateTime.Now, utcHourOffset);

            // display the threadid
            string log = string.Format(@"{0} [{1}] {2}", dateWithOffset, Thread.CurrentThread.ManagedThreadId, message);

            switch (eventType)
            {
                case TraceEventType.Critical:
                case TraceEventType.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(log);
                    Console.ResetColor();
                    break;

                case TraceEventType.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(log);
                    Console.ResetColor();
                    break;

                default:
                    Console.WriteLine(log);
                    break;
            }
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            this.TraceEvent(eventCache, source, eventType, id, message, new object[] {});
        }

        public override void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        public override void Write(string message)
        {
            Console.Write(message);
        }
    }
}
