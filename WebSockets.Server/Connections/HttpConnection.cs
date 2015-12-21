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
        private readonly string _webRoot;

        public HttpConnection(NetworkStream networkStream, string path, string webRoot)
        {
            _networkStream = networkStream;
            _path = path;
            _webRoot = webRoot;
        }

        private static bool IsDirectory(string file)
        {
            if (Directory.Exists(file))
            {
                //detect whether its a directory or file
                FileAttributes attr = File.GetAttributes(file);
                return ((attr & FileAttributes.Directory) == FileAttributes.Directory);
            }

            return false;
        }

        public void Respond()
        {
            string file = GetSafePath(_path);

            // default to index.html is path is supplied
            if (IsDirectory(file))
            {
                file += "index.html";
            }

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
                    Trace.TraceInformation("Served file: " + file);
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

        /// <summary>
        /// I am not convinced that this function is indeed safe from hacking file path tricks
        /// </summary>
        /// <param name="path">The relative path</param>
        /// <returns>The file system path</returns>
        private string GetSafePath(string path)
        {
            path = path.Trim().Replace("/","\\");
            if (path.Contains("..") || !path.StartsWith("\\") || path.Contains(":"))
            {
                return string.Empty;
            }

            string file = _webRoot + path;
            return file;
        }

        public void RespondMimeTypeFailure(string file)
        {
            HttpHelper.WriteHttpHeader("415 Unsupported Media Type", _networkStream);
            Trace.TraceWarning("File extension not found MimeTypes.config: " + file);
        }

        public void RespondNotFoundFailure(string file)
        {
            HttpHelper.WriteHttpHeader("HTTP/1.1 404 Not Found", _networkStream);
            Trace.TraceInformation("File not found: " + file);
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
