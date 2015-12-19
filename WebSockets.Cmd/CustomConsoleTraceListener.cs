using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace WebSockets.Cmd
{
    public class CustomConsoleTraceListener : TraceListener
    {
        public override void WriteLine(string message)
        {
            // write the localised date and time but include the time zone in brackets (good for combining logs from different timezones)
            TimeSpan utcOffset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
            string utcHourOffset = utcOffset.TotalHours == 0 ? string.Empty : string.Format(" ({0}{1:hh})", ((utcOffset < TimeSpan.Zero) ? "-" : "+"), utcOffset);
            string dateWithOffset = string.Format(@"{0:yyyy/MM/dd hh:mm:ss.fff}{1}", DateTime.Now, utcHourOffset);

            // display the threadid
            string log = string.Format(@"{0} [{1}] {2}", dateWithOffset, Thread.CurrentThread.ManagedThreadId, message);
            Console.WriteLine(log);
        }

        public override void Write(string message)
        {
            // for some reason Trace.Error uses this function
            Console.ForegroundColor = ConsoleColor.Red;
            WriteLine(message);
            Console.ResetColor();
        }
    }
}
