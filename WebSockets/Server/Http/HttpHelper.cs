using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;
using WebSockets.Exceptions;

namespace WebSockets.Server.Http
{
    public class HttpHelper
    {
        public static string ReadHttpHeader(Stream stream)
        {
            int length = 1024*16; // 16KB buffer more than enough for http header
            byte[] buffer = new byte[length];
            int offset = 0;
            int bytesRead = 0;
            do
            {
                if (offset >= length)
                {
                    throw new EntityTooLargeException("Http header message too large to fit in buffer (16KB)");
                }

                bytesRead = stream.Read(buffer, offset, length - offset);
                offset += bytesRead;
                string header = Encoding.UTF8.GetString(buffer, 0, offset);

                // as per http specification, all headers should end this this
                if (header.Contains("\r\n\r\n"))
                {
                    return header;
                }

            } while (bytesRead > 0);

            return string.Empty;
        }

        public static void WriteHttpHeader(string response, Stream stream)
        {
            response = response.Trim() + Environment.NewLine + Environment.NewLine;
            Byte[] bytes = Encoding.UTF8.GetBytes(response);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
