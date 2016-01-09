using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Diagnostics;

namespace WebSockets.Server.WebSocketProtocol
{
    //  see http://tools.ietf.org/html/rfc6455 for specification

    public class WebSocketFrameReader
    {
        public WebSocketFrame Read(NetworkStream stream)
        {
            // read the first byte. If the connection has been terminated abnormally then this would return an FF and we should exit
            byte byte1 = (byte)stream.ReadByte();

            // this condition will happen if the connection has terminated unexpectedly
            if (!stream.DataAvailable && byte1 == 0xFF)
            {
                return new WebSocketFrame(true, WebSocketOpCode.ConnectionClose, new byte[0], false);
            }

            // process first byte
            byte finBitFlag = 0x80;
            byte opCodeFlag = 0x0F;
            bool isFinBitSet = (byte1 & finBitFlag) == finBitFlag;
            WebSocketOpCode opCode = (WebSocketOpCode)(byte1 & opCodeFlag);

            // read and process second byte
            byte byte2 = (byte)stream.ReadByte();
            byte maskFlag = 0x80;
            bool isMaskBitSet = (byte2 & maskFlag) == maskFlag;
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
                for (int i = 0; i < encodedPayload.Length; i++)
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
            uint len = (uint)(byte2 & payloadLenFlag);

            // read a short length or a long length depending on the value of len
            if (len == 126)
            {
                byte[] lenBuffer = BinaryReaderWriter.ReadExactly(2, stream);
                Array.Reverse(lenBuffer); // big endian
                len = (uint)BitConverter.ToUInt16(lenBuffer, 0);
            }
            else if (len == 127)
            {
                byte[] lenBuffer = BinaryReaderWriter.ReadExactly(8, stream);
                Array.Reverse(lenBuffer); // big endian
                len = (uint)BitConverter.ToUInt64(lenBuffer, 0);
                const uint maxLen = 2147483648; // 2GB

                // protect ourselves against bad data
                if (len > maxLen || len < 0)
                {
                    throw new ArgumentOutOfRangeException(string.Format("Payload length out of range. Min 0 max 2GB. Actual {0:#,##0} bytes.", len));
                }
            }

            return len;
        }
    }
}
