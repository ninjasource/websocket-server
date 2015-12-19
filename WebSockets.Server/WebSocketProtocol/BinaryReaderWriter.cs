using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WebSockets.Server.WebSocketProtocol
{
    class BinaryReaderWriter
    {
        public static byte[] ReadExactly(int length, Stream stream)
        {
            byte[] buffer = new byte[length];
            if (length == 0)
            {
                return buffer;
            }

            int offset = 0;
            do
            {
                int bytesRead = stream.Read(buffer, offset, length - offset);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException(string.Format("Unexpected end of stream encountered whilst attempting to read {0:#,##0} bytes", length));
                }

                offset += bytesRead;
            } while (offset < length);

            return buffer;
        }

        public static void WriteULong(ulong value, Stream stream)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            stream.Write(buffer, 0, buffer.Length);
        }

        public static void WriteUShort(ushort value, Stream stream)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            stream.Write(buffer, 0, buffer.Length);
        }
    }
}
