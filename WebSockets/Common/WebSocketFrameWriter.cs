using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.Sockets;

namespace WebSockets.Common
{
    // see http://tools.ietf.org/html/rfc6455 for specification
    // see fragmentation section for sending multi part messages
    // EXAMPLE: For a text message sent as three fragments, 
    //   the first fragment would have an opcode of TextFrame and isLastFrame false,
    //   the second fragment would have an opcode of ContinuationFrame and isLastFrame false,
    //   the third fragment would have an opcode of ContinuationFrame and isLastFrame true.

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
                byte finBitSetAsByte = isLastFrame ? (byte) 0x80 : (byte) 0x00;
                byte byte1 = (byte) (finBitSetAsByte | (byte) opCode);
                memoryStream.WriteByte(byte1);

                // NB, dont set the mask flag. No need to mask data from server to client
                // depending on the size of the length we want to write it as a byte, ushort or ulong
                if (payload.Length < 126)
                {
                    byte byte2 = (byte) payload.Length;
                    memoryStream.WriteByte(byte2);
                }
                else if (payload.Length <= ushort.MaxValue)
                {
                    byte byte2 = 126;
                    memoryStream.WriteByte(byte2);
                    BinaryReaderWriter.WriteUShort((ushort) payload.Length, memoryStream, false);
                }
                else
                {
                    byte byte2 = 127;
                    memoryStream.WriteByte(byte2);
                    BinaryReaderWriter.WriteULong((ulong) payload.Length, memoryStream, false);
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
    }
}
