using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Xml;

namespace WebSockets.Server.Http
{
    class MimeTypes : Dictionary<string,string>
    {
        public MimeTypes(string webRoot)
        {
            string configFileName = webRoot + @"\MimeTypes.config";
            if (!File.Exists(configFileName))
            {
                throw new FileNotFoundException("Mime Types config file not found: " + configFileName);
            }

            try
            {
                XmlDocument document = new XmlDocument();
                document.Load(configFileName);
                foreach (XmlNode node in document.SelectNodes("configuration/system.webServer/staticContent/mimeMap"))
                {
                    string fileExtension = node.Attributes["fileExtension"].Value;
                    string mimeType = node.Attributes["mimeType"].Value;
                    this.Add(fileExtension, mimeType);
                }
            }
            catch (Exception ex)
            {
                throw new ConfigurationErrorsException("Invalid Mime Types configuration file: " + configFileName, ex);
            }
        }
    }
}
