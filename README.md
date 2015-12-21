<h1>WebSocket Server in c#</h1>

<p>Set <code>WebSockets.Cmd</code> as the startup project</p>

<h2>License</h2>

The MIT License (MIT)
<br/>See LICENCE.txt

<h2>Introduction</h2>

<p>A lot of the Web Socket examples out there are for old Web Socket versions and included complicated code (and external libraries) for fall back communication. All modern browsers that anyone cares about (including safari on an iphone) support at least <strong>version 13 of the Web Socket protocol </strong>so I&#39;d rather not complicate things. This is a bare bones implementation of the web socket protocol in C# with no external libraries involved. You can connect using standard HTML5 JavaScript.</p>

<p>This application serves up basic html pages as well as handling WebSocket connections. This may seem confusing but it allows you to send the client the html they need to make a web socket connection and also allows you to share the same port. However, the <code>HttpConnection</code> is very rudimentary. I&#39;m sure it has some glaring security problems. It was just made to make this demo easier to run. Replace it with your own or don&#39;t use it.</p>

<h2>Background</h2>

<p>There is nothing magical about Web Sockets. The spec is easy to follow and there is no need to use special libraries. At one point, I was even considering somehow communicating with Node.js but that is not necessary. The spec can be a bit fiddly with bits and bytes but this was probably done to keep the overheads low. This is my first CodeProject article and I hope you will find it easy to follow. The following links offer some great advice:</p>

<p>Step by step guide</p>

<ul>
	<li><a href="https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_servers">https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_servers</a></li>
</ul>

<p>The official Web Socket spec</p>

<ul>
	<li><a href="http://tools.ietf.org/html/rfc6455">http://tools.ietf.org/html/rfc6455</a></li>
</ul>

<p>Some useful stuff in C#</p>

<ul>
	<li><a href="https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_server">https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_server</a></li>
</ul>

<h2>Using the Code</h2>

<p>A good place to put a breakpoint is in the <code>WebServer</code> class in the <code>HandleAsyncConnection</code> function. Note that this is a multithreaded server so&nbsp;you may want to freeze threads if this gets confusing. The console output prints the thread id to make things easier. If you want to skip past all the plumbing, then another good place to start is the <code>Respond</code> function in the <code>WebSocketConnection</code> class. If you are not interested in the inner workings of Web Sockets and just want to use them, then take a look at the <code>OnTextFrame</code> in the <code>ChatWebSocketConnection</code> class. See below.</p>

<p>Implementation of a chat web socket connection:</p>

<pre lang="cs">
class ChatWebSocketConnection : WebSocketConnection
{
    public ChatWebSocketConnection(NetworkStream networkStream, TcpClient tcpClient, string header)
        : base(networkStream, header)
    {
        // send requests immediately if true 
        // (needed for small low latency packets but not a long stream). 
        // Basically, don&#39;t wait for buffer to be full
        tcpClient.NoDelay = true;
    }

    protected override void OnTextFrame(string text)
    {
        string response = &quot;Server: &quot; + text;
        base.Writer.WriteText(response);
        Trace.WriteLine(response);
    }
}</pre>

<p>The factory used to create the connection is as follows:</p>

<pre lang="cs">
class ConnectionFactory : IConnectionFactory
{
    public IConnection CreateInstance(ConnectionDetails connectionDetails)
    {
        switch (connectionDetails.ConnectionType)
        {
            case ConnectionType.WebSocket:
                // you can support different kinds of web socket connections using a different path
                if (connectionDetails.Path == &quot;/chat&quot;)
                {
                    return new ChatWebSocketConnection(connectionDetails.NetworkStream, 
				connectionDetails.TcpClient, connectionDetails.Header);
                }
                break;
            case ConnectionType.Http:
                // this path actually refers to the relative location of some html file or image
                return new HttpConnection(connectionDetails.NetworkStream, connectionDetails.Path);
        }

        return new BadRequestConnection(connectionDetails.NetworkStream, connectionDetails.Header);
    }
}</pre>

<p>HTML5 JavaScript used to connect:</p>

<pre lang="jscript">
// open the connection to the Web Socket server
var CONNECTION = new WebSocket(&#39;ws://localhost/chat&#39;);

// Log messages from the server
CONNECTION.onmessage = function (e) {
    console.log(e.data);
};
        
CONNECTION.send(&#39;Hellow World&#39;);</pre>

<p>Starting the server:</p>

<pre lang="cs">
static void Main(string[] args)
{
    // used to decide what to do with incoming connections
    ConnectionFactory connectionFactory = new ConnectionFactory();

    using (WebServer server = new WebServer(connectionFactory))
    {
        // listen on port 80
        server.Listen(80);
        Console.ReadKey();
    }
}</pre>

<h2>Web Socket Protocol</h2>

<p>The first thing to realize about the protocol is that it is, in essence, a basic duplex TCP/IP socket connection. The connection starts off with the client connecting to a remote server and sending http header text to that server. The header text asks the web server to upgrade the connection to a web socket connection. This is done as a handshake where the web server responds with an appropriate http text header and from then onwards, the client and server will talk the Web Socket language.</p>

<h3>Handshake</h3>

<pre lang="cs">
Regex webSocketKeyRegex = new Regex(&quot;Sec-WebSocket-Key: (.*)&quot;);
Regex webSocketVersionRegex = new Regex(&quot;Sec-WebSocket-Version: (.*)&quot;);

// check the version. Support version 13 and above
const int WebSocketVersion = 13;
int secWebSocketVersion = Convert.ToInt32(webSocketVersionRegex.Match(header).Groups[1].Value.Trim());
if (secWebSocketVersion &lt; WebSocketVersion)
{
    throw new WebSocketVersionNotSupportedException(string.Format
    (&quot;WebSocket Version {0} not supported. Must be {1} or above&quot;, secWebSocketVersion, WebSocketVersion));
}

string secWebSocketKey = webSocketKeyRegex.Match(header).Groups[1].Value.Trim();
string setWebSocketAccept = WebSocketHandshakeHelper.ComputeSocketAcceptString(secWebSocketKey);
string response = (&quot;HTTP/1.1 101 Switching Protocols&quot; + Environment.NewLine
                           + &quot;Connection: Upgrade&quot; + Environment.NewLine
                           + &quot;Upgrade: websocket&quot; + Environment.NewLine
                           + &quot;Sec-WebSocket-Accept: &quot; + setWebSocketAccept);

HttpHelper.WriteHttpHeader(response, networkStream);</pre>

<p>This computes the <code>accept string</code>:</p>

<pre lang="cs">
/// &lt;summary&gt;
/// Combines the key supplied by the client with a guid and returns the sha1 hash of the combination
/// &lt;/summary&gt;
public static string ComputeSocketAcceptString(string secWebSocketKey)
{
    // this is a guid as per the web socket spec
    const string webSocketGuid = &quot;258EAFA5-E914-47DA-95CA-C5AB0DC85B11&quot;;

    string concatenated = secWebSocketKey + webSocketGuid;
    byte[] concatenatedAsBytes = Encoding.UTF8.GetBytes(concatenated);
    byte[] sha1Hash = SHA1.Create().ComputeHash(concatenatedAsBytes);
    string secWebSocketAccept = Convert.ToBase64String(sha1Hash);
    return secWebSocketAccept;
}</pre>

<h3>Reading and Writing</h3>

<p>After the handshake as been performed, the server goes into a <code>read</code> loop. The following class reads Web Socket frames from the network stream:</p>

<pre lang="cs">
public class WebSocketFrameReader
{
    public WebSocketFrame Read(NetworkStream stream)
    {
        // read the first byte. If the connection has been terminated abnormally 
	// then this would return an FF and we should exit
        byte byte1 = (byte)stream.ReadByte();

        // this condition will happen if the connection has terminated unexpectedly
        if (!stream.DataAvailable &amp;&amp; byte1 == 0xFF)
        {
            return new WebSocketFrame(true, WebSocketOpCode.ConnectionClose, new byte[0], false);
        }

        // process first byte
        byte finBitFlag = 0x80;
        byte opCodeFlag = 0x0F;
        bool isFinBitSet = (byte1 &amp; finBitFlag) == finBitFlag;
        WebSocketOpCode opCode = (WebSocketOpCode)(byte1 &amp; opCodeFlag);

        // read and process second byte
        byte byte2 = (byte)stream.ReadByte();
        byte maskFlag = 0x80;
        bool isMaskBitSet = (byte2 &amp; maskFlag) == maskFlag;
        uint len = ReadLength(byte2, stream);
        byte[] decodedPayload;

        // use the masking key to decode the data if needed
        if (isMaskBitSet)
        {
            const int maskKeyLen = 4;
            byte[] maskKey = BinaryReaderWriter.ReadExactly(maskKeyLen, stream);
            byte[] encodedPayload = BinaryReaderWriter.ReadExactly((int)len, stream);
            decodedPayload = new byte[len];

            // apply the mask key
            for (int i = 0; i &lt; encodedPayload.Length; i++)
            {
                decodedPayload[i] = (Byte)(encodedPayload[i] ^ maskKey[i % maskKeyLen]);
            }
        }
        else
        {
            decodedPayload = BinaryReaderWriter.ReadExactly((int)len, stream);
        }

        WebSocketFrame frame = new WebSocketFrame(isFinBitSet, opCode, decodedPayload, true);
        return frame;
    }
    
    private static uint ReadLength(byte byte2, Stream stream)
    {
        byte payloadLenFlag = 0x7F;
        uint len = (uint)(byte2 &amp; payloadLenFlag);

        // read a short length or a long length depending on the value of len
        if (len == 126)
        {
            byte[] lenBuffer = BinaryReaderWriter.ReadExactly(2, stream);
            len = (uint)BitConverter.ToUInt16(lenBuffer, 0);
        }
        else if (len == 127)
        {
            byte[] lenBuffer = BinaryReaderWriter.ReadExactly(8, stream);
            len = (uint)BitConverter.ToUInt64(lenBuffer, 0);
            const uint maxLen = 2147483648; // 2GB

            // protect ourselves against bad data
            if (len &gt; maxLen || len &lt; 0)
            {
                throw new ArgumentOutOfRangeException(string.Format
		(&quot;Payload length out of range. Min 0 max 2GB. Actual {0:#,##0} bytes.&quot;, len));
            }
        }

        return len;
    }    
}</pre>

<p>If we want to write data to the network stream, we use the following class:</p>

<pre lang="cs">
public class WebSocketFrameWriter
{
    private readonly Stream _stream;

    public WebSocketFrameWriter(Stream stream)
    {
        _stream = stream;
    }

    public void Write(WebSocketOpCode opCode, byte[] payload, bool isLastFrame)
    {
        // best to write everything to a memory stream before we push it onto the wire
        // not really necessary but I like it this way
        using (MemoryStream memoryStream = new MemoryStream())
        {
            byte finBitSetAsByte = isLastFrame ? (byte)0x80 : (byte)0x00;
            byte byte1 = (byte)(finBitSetAsByte | (byte)opCode);
            memoryStream.WriteByte(byte1);

            // NB, dont set the mask flag. No need to mask data from server to client
            // depending on the size of the length we want to write it as a byte, ushort or ulong
            if (payload.Length &lt; 126)
            {
                byte byte2 = (byte)payload.Length;
                memoryStream.WriteByte(byte2);
            }
            else if (payload.Length &lt;= ushort.MaxValue)
            {
                byte byte2 = 126;
                memoryStream.WriteByte(byte2);
                BinaryReaderWriter.WriteUShort((ushort)payload.Length, memoryStream);
            }
            else
            {
                byte byte2 = 127;
                memoryStream.WriteByte(byte2);
                BinaryReaderWriter.WriteULong((ulong)payload.Length, memoryStream);
            }

            memoryStream.Write(payload, 0, payload.Length);
            byte[] buffer = memoryStream.ToArray();
            _stream.Write(buffer, 0, buffer.Length);
        }            
    }

    public void Write(WebSocketOpCode opCode, byte[] payload)
    {
        Write(opCode, payload, true);
    }

    public void WriteText(string text)
    {
        byte[] responseBytes = Encoding.UTF8.GetBytes(text);
        Write(WebSocketOpCode.TextFrame, responseBytes);
    }    
}</pre>

<h2>Points of Interest</h2>

<p>Problems with Proxy Servers:<br />
Proxy servers which have not been configured to support Web sockets will not work well with them.<br />
I suggest that you use transport layer security if you want this to work across the wider internet especially from within a corporation.</p>

<h2>History</h2>

<ul>
	<li>Version 1.0 WebSocket</li>
</ul>
