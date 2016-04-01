using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSockets.Server.Http
{
    internal class MimeTypesFactory
    {
        private static Dictionary<string, MimeTypes> _mimeTypes = new Dictionary<string, MimeTypes>();

        public static MimeTypes GetMimeTypes(string webRoot)
        {
            lock (_mimeTypes)
            {
                MimeTypes mimeTypes;
                if (!_mimeTypes.TryGetValue(webRoot, out mimeTypes))
                {
                    mimeTypes = new MimeTypes(webRoot);
                    _mimeTypes.Add(webRoot, mimeTypes);
                }

                return mimeTypes;
            }
        }
    }
}
