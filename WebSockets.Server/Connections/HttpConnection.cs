using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Reflection;
using System.Configuration;
using System.Xml;

namespace WebSockets.Server.Connections
{
    public class HttpConnection : IConnection
    {
        private readonly NetworkStream _networkStream;
        private readonly string _path;
        

        public HttpConnection(NetworkStream networkStream, string path)
        {
            _networkStream = networkStream;
            _path = path;
        }

        public void Respond()
        {
            string file = GetSafePath(_path);
            FileInfo fi = new FileInfo(file);
            
            if (fi.Exists)
            {
                string ext = fi.Extension.ToLower();
                string contentType;
                if (MimeTypes.Instance.TryGetValue(ext, out contentType))
                {
                    Byte[] bytes = File.ReadAllBytes(fi.FullName);
                    RespondSuccess(contentType, bytes.Length);
                    _networkStream.Write(bytes, 0, bytes.Length);
                    Trace.WriteLine("Served file: " + file);
                }
                else
                {
                    RespondMimeTypeFailure(file);
                }
            }
            else
            {
                RespondNotFoundFailure(file);
            }
        }

        private static string GetBasePath()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase).Replace(@"file:\", string.Empty);
        }

        /// <summary>
        /// I am not convinced that this function is indeed safe from hacking file path tricks
        /// </summary>
        /// <param name="path">The relative path</param>
        /// <returns>The file system path</returns>
        private static string GetSafePath(string path)
        {
            path = path.Trim().Replace("/","\\");
            if (path.Contains("..") || !path.StartsWith("\\") || path.Contains(":"))
            {
                return string.Empty;
            }

            string file = GetBasePath() + path;
            return file;
        }

        public void RespondMimeTypeFailure(string file)
        {
            HttpHelper.WriteHttpHeader("415 Unsupported Media Type", _networkStream);
            Trace.WriteLine("File extension not found in MIME types: " + file);
        }

        public void RespondNotFoundFailure(string file)
        {
            HttpHelper.WriteHttpHeader("HTTP/1.1 404 Not Found", _networkStream);
            Trace.WriteLine("File not found: " + file);
        }

        public void RespondSuccess(string contentType, int contentLength)
        {
            string response = "HTTP/1.1 200 OK" + Environment.NewLine +
                              "Content-Type: " + contentType + Environment.NewLine +
                              "Content-Length: " + contentLength + Environment.NewLine +
                              "Connection: close";
            HttpHelper.WriteHttpHeader(response, _networkStream);
        }

        public void Dispose()
        {
            // do nothing. The network stream will be closed by the WebServer
        }
    }
}
