using System.Collections.Generic;
using System.IO;
using NLog;
using OpenPop.Mime;

namespace TopCrawler
{
    /// <summary>
    /// Singleton: service class with general functions
    /// </summary>
    class Functions
    {
        public static Logger Log = LogManager.GetLogger("Global");
        public static Logger Archive = LogManager.GetLogger("Archivator");
        public static ZabbixSender Zabbix;
        public static Dictionary<string, Counter> ProcessedCounters = new Dictionary<string, Counter>();
        public static Dictionary<string, Counter> ErrorsCounters = new Dictionary<string, Counter>();

        public static string MessageToString(Message message)
        {
            using (var stream = new MemoryStream())
            {
                message.Save(stream);
                stream.Position = 0;
                var reader = new StreamReader(stream);

                return reader.ReadToEnd();
            }
        }
    }
}
