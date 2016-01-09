using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using WebSockets.Server;
using WebSockets.Cmd.Connections;
using System.Diagnostics;
using WebSockets.Cmd.Properties;

namespace WebSockets.Cmd
{
    public class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                string webRoot = Settings.Default.WebRoot;
                int port = Settings.Default.Port;

                // used to decide what to do with incoming connections
                ConnectionFactory connectionFactory = new ConnectionFactory(webRoot);

                using (WebServer server = new WebServer(connectionFactory))
                {
                    server.Listen(port);
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                Console.ReadKey();
            }
        }
    }
}
