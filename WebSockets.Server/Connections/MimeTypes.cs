using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Xml;

namespace WebSockets.Server.Connections
{
    class MimeTypes : Dictionary<string,string>
    {
        private static object _locker = new object();
        private static MimeTypes _instance;
        
        private static string GetBasePath()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase).Replace(@"file:\", string.Empty);
        }

        private MimeTypes()
        {
            string configFileName = GetBasePath() + @"\MimeTypes.config";
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

        public static MimeTypes Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_locker)
                    {
                        if (_instance == null)
                        {
                            _instance = new MimeTypes();
                        }
                    }
                }

                return _instance;
            }
        }
    }
}
