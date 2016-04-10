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
using WebSockets.Common;

namespace WebSockets.Server.Http
{
    public class HttpService : IService
    {
        private readonly Stream _stream;
        private readonly string _path;
        private readonly string _webRoot;
        private readonly IWebSocketLogger _logger;
        private readonly MimeTypes _mimeTypes;

        public HttpService(Stream stream, string path, string webRoot, IWebSocketLogger logger)
        {
            _stream = stream;
            _path = path;
            _webRoot = webRoot;
            _logger = logger;
            _mimeTypes = MimeTypesFactory.GetMimeTypes(webRoot);
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
            _logger.Information(this.GetType(), "Request: {0}", _path);
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
                if (_mimeTypes.TryGetValue(ext, out contentType))
                {
                    Byte[] bytes = File.ReadAllBytes(fi.FullName);
                    RespondSuccess(contentType, bytes.Length);
                    _stream.Write(bytes, 0, bytes.Length);
                    _logger.Information(this.GetType(), "Served file: {0}", file);
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
            path = path.Trim().Replace("/", "\\");
            if (path.Contains("..") || !path.StartsWith("\\") || path.Contains(":"))
            {
                return string.Empty;
            }

            string file = _webRoot + path;
            return file;
        }

        public void RespondMimeTypeFailure(string file)
        {
            HttpHelper.WriteHttpHeader("415 Unsupported Media Type", _stream);
            _logger.Warning(this.GetType(), "File extension not found MimeTypes.config: {0}", file);
        }

        public void RespondNotFoundFailure(string file)
        {
            HttpHelper.WriteHttpHeader("HTTP/1.1 404 Not Found", _stream);
            _logger.Information(this.GetType(), "File not found: {0}", file);
        }

        public void RespondSuccess(string contentType, int contentLength)
        {
            string response = "HTTP/1.1 200 OK" + Environment.NewLine +
                              "Content-Type: " + contentType + Environment.NewLine +
                              "Content-Length: " + contentLength + Environment.NewLine +
                              "Connection: close";
            HttpHelper.WriteHttpHeader(response, _stream);
        }

        public void Dispose()
        {
            // do nothing. The network stream will be closed by the WebServer
        }
    }
}
