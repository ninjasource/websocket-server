Thanks to help from the following websites:

Step by step guide
https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_servers

The official Web Socket spec
http://tools.ietf.org/html/rfc6455 

Some useful stuff in c#
https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_server

Web Socket Protocol 13 (and above) supported

To run:
Run the console application
NOTE You will get a firewall warning because you are listening on a port. This is normal for any socket based server.
The console application will run a self test so you can see if anything is wrong. The self test will open a connection, perform the open handshake, send a message, receive a response and start the close handshake and disconnect.

If the self test is a success then the following should work too:
Open a browser and enter: http://localhost/client.html
The web server will then serve up the web page requested (client.html), 
  The javascript in that webpage will execute and attempt to make a WebSocket connection to that same server and port (80)
  At this point the webserver will upgrade the connection to a web socket connection and you can chat with the server
If you want to access this from another machine then make sure your firewall is not blocking the port

Note: 
A lot of the Web Socket examples out there are for old Web Socket versions and included complicated code for fall back communication. 
All modern browsers (including safari on an iphone) support at least version 13 of the Web Socket protocol so I'd rather not complicate things.
This application serves up basic html pages as well as handling WebSocket connections. 
This may seem confusing but it allows you to send the client the html they need to make a web socket connection and also allows you to share the same port
However, the HttpConnection is very rudimentary. I'm sure it has some glaring security problems. It was just made to make this demo easier to run. Replace it with your own or dont use it.

Debugging:
A good place to put a breakpoint is in the WebServer class in the HandleAsyncConnection function. 
Note that this is a multithreaded server to you may want to freeze threads if this gets confusing. The console output prints the thread id to make things easier
If you want to skip past all the plumbing then another good place to start is the Respond function in the WebSocketService class
If you are not interested in the inner workings of Web Sockets and just want to use them then take a look at the OnTextFrame in the ChatWebSocketService class

Problems with Proxy Servers:
Proxy servers which have not been configured to support Web sockets will not work well with them. 
I suggest that you use transport layer security (SSL) if you want this to work across the wider internet especially from within a corporation

Sub Folders:
You can assign different web socket handlers depending on the folder attribute of the first line of the http request
eg: 
GET /chat HTTP/1.1
This folder has been setup to use the ChatWebSocketService class

Change Log:

10 Apr 2016: SSL support
02 Apr 2016: c# Client
