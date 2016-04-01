using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using WebSockets.Server;
using System.Diagnostics;
using WebSocketsCmd.Client;
using WebSocketsCmd.Properties;
using WebSockets.Client;
using System.Threading.Tasks;
using WebSockets.Common;
using System.Threading;
using WebSockets;
using WebSockets.Events;
using WebSocketsCmd.Server;

namespace WebSocketsCmd
{
    public class Program
    {
        private static void TestClient(object state)
        {
            var logger = (IWebSocketLogger) state;
            using (var client = new ChatWebSocketClient(true, logger))
            {
                Uri uri = new Uri("ws://localhost/chat");
                client.TextFrame += Client_TextFrame;
                client.ConnectionOpened += Client_ConnectionOpened;

                // test the open handshake
                client.OpenBlocking(uri);
            }

            Trace.TraceInformation("Client finished, press any key");
            Console.ReadKey();
        }

        private static void Client_ConnectionOpened(object sender, EventArgs e)
        {
            Trace.TraceInformation("Client: Connection Opened");
            var client = (ChatWebSocketClient) sender;

            // test sending a message to the server
            client.Send("Hi");
        }

        private static void Client_TextFrame(object sender, TextFrameEventArgs e)
        {
            Trace.TraceInformation("Client: {0}", e.Text);
            var client = (ChatWebSocketClient) sender;

            // lets test the close handshake
            client.Dispose();
        }

        private static void Main(string[] args)
        {
            IWebSocketLogger logger = new WebSocketLogger();

            try
            {
                string webRoot = Settings.Default.WebRoot;
                int port = Settings.Default.Port;

                // used to decide what to do with incoming connections
                ServiceFactory serviceFactory = new ServiceFactory(webRoot, logger);

                using (WebServer server = new WebServer(serviceFactory, logger))
                {
                    server.Listen(port);
                    Thread clientThread = new Thread(new ParameterizedThreadStart(TestClient));
                    clientThread.IsBackground = false;
                    clientThread.Start(logger);
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                logger.Error(null, ex);
                Console.ReadKey();
            }
        }
    }
}
