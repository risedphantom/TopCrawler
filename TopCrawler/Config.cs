using System;
using System.Collections.Generic;
using System.Configuration;

namespace TopCrawler
{
    #region --- Types ---
    static class MailboxType
    {
        public const string Bo = "bo";
        public const string Crm = "crm";
        public const string Fbl = "fbl";
        public const string Bounce = "bounce";
    }

    class MailboxInfo
    {
        public string Type { get; set; }
        public string Hostname { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
    }

    class DataBlockOptions
    {
        public int Maxdop { get; set; }
        public int BoundedCapacity { get; set; }

        public DataBlockOptions()
        {
            Maxdop = 1;
            BoundedCapacity = 1;
        }
    }
    #endregion

    #region --- Config reader ---
    /// <summary>
    /// Custom config section
    /// </summary>
    public class CustomSettingsConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("CredentialsList")]
        public CredentialsCollection CredentialItems
        {
            get { return base["CredentialsList"] as CredentialsCollection; }
        }

        [ConfigurationProperty("DataFlowOptionsList")]
        public DataBlockOptionsCollection DataFlowOptionsItems
        {
            get { return base["DataFlowOptionsList"] as DataBlockOptionsCollection; }
        }
    }

    /// <summary>
    /// Custom collection - credentials list
    /// </summary>
    [ConfigurationCollection(typeof(CredentialsElement), AddItemName = "credentials")]
    public class CredentialsCollection : ConfigurationElementCollection, IEnumerable<CredentialsElement>
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new CredentialsElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((CredentialsElement)element).Username;
        }

        public CredentialsElement this[int index]
        {
            get { return BaseGet(index) as CredentialsElement; }
        }

        public new IEnumerator<CredentialsElement> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return BaseGet(i) as CredentialsElement;
            }
        }
    }

    /// <summary>
    /// Custom credentials item
    /// </summary>
    public class CredentialsElement : ConfigurationElement
    {
        [ConfigurationProperty("hostname", DefaultValue = "")]
        public string Hostname
        {
            get { return base["hostname"] as string; }
        }

        [ConfigurationProperty("username", DefaultValue = "", IsKey = true)]
        public string Username
        {
            get { return base["username"] as string; }
        }

        [ConfigurationProperty("password", DefaultValue = "")]
        public string Password
        {
            get { return base["password"] as string; }
        }

        [ConfigurationProperty("type", DefaultValue = "")]
        public string Type
        {
            get { return base["type"] as string; }
        }

        [ConfigurationProperty("port", DefaultValue = "")]
        public string Port
        {
            get { return base["port"] as string; }
        }
    }

    /// <summary>
    /// Custom collection - DataBlock options list
    /// </summary>
    [ConfigurationCollection(typeof(DataBlockOptionsElement), AddItemName = "datablockoptions")]
    public class DataBlockOptionsCollection : ConfigurationElementCollection, IEnumerable<DataBlockOptionsElement>
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new DataBlockOptionsElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((DataBlockOptionsElement)element).Name;
        }

        public CredentialsElement this[int index]
        {
            get { return BaseGet(index) as CredentialsElement; }
        }

        public new IEnumerator<DataBlockOptionsElement> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return BaseGet(i) as DataBlockOptionsElement;
            }
        }
    }

    /// <summary>
    /// Custom DataBlock options item
    /// </summary>
    public class DataBlockOptionsElement : ConfigurationElement
    {
        [ConfigurationProperty("name", DefaultValue = "", IsKey = true)]
        public string Name
        {
            get { return base["name"] as string; }
        }

        [ConfigurationProperty("maxdop", DefaultValue = "")]
        public string Maxdop
        {
            get { return base["maxdop"] as string; }
        }

        [ConfigurationProperty("boundedcapacity", DefaultValue = "")]
        public string BoundedCapacity
        {
            get { return base["boundedcapacity"] as string; }
        }
    }
    #endregion

    /// <summary>
    /// Current preferences
    /// </summary>
    class Config
    {
        public List<MailboxInfo> CredentialsList { get; private set; }
        public Dictionary<string, DataBlockOptions> DataFlowOptionsList { get; private set; }
        public string PluginDirectory { get; private set; }
        public string CrmConnection { get; private set; }
        public string MailWhConnection { get; private set; }
        public string ProcessedCountKey { get; private set; }
        public string TimingKey { get; private set; }
        public string ErrorKey { get; private set; }
        public string HostKey { get; private set; }
        public string ZabbixServer { get; private set; }
        public int ZabbixPort { get; private set; }
        public int NotifyPeriod { get; private set; }
        public bool DeleteMail { get; private set; }

        public Config()
        {
            CredentialsList = new List<MailboxInfo>();
            DataFlowOptionsList = new Dictionary<string, DataBlockOptions>();
        }

        public DataBlockOptions GetDataBlockOptions(string blockName)
        {
            DataBlockOptions value;
            DataFlowOptionsList.TryGetValue(blockName, out value);
            return value ?? new DataBlockOptions();
        }

        public void Read()
        {
            var customConfig = (CustomSettingsConfigSection)ConfigurationManager.GetSection("CustomSettings");

            //Get mailboxes
            foreach (var item in customConfig.CredentialItems)
                CredentialsList.Add(new MailboxInfo
                {
                    Hostname = item.Hostname,
                    Port = Convert.ToInt32(item.Port),
                    User = item.Username,
                    Type = item.Type,
                    Password = item.Password
                });

            //Get DataFlow settings
            foreach (var item in customConfig.DataFlowOptionsItems)
                DataFlowOptionsList.Add(item.Name, new DataBlockOptions
                {
                    Maxdop = Convert.ToInt32(item.Maxdop),
                    BoundedCapacity = Convert.ToInt32(item.BoundedCapacity)
                });

            //Get Zabbix settings
            ZabbixServer = ConfigurationManager.AppSettings["zabbix.server"];
            ZabbixPort = Convert.ToInt32(ConfigurationManager.AppSettings["zabbix.port"]);
            HostKey = ConfigurationManager.AppSettings["hostkey"];
            NotifyPeriod = Convert.ToInt32(ConfigurationManager.AppSettings["notifyperiod"]);
            ProcessedCountKey = ConfigurationManager.AppSettings["processedcountkey"] + NotifyPeriod;
            ErrorKey = ConfigurationManager.AppSettings["errorkey"] + NotifyPeriod;
            TimingKey = ConfigurationManager.AppSettings["timingkey"];
            
            //Other settings
            DeleteMail = Convert.ToBoolean(ConfigurationManager.AppSettings["deletemail"]);
            PluginDirectory = AppDomain.CurrentDomain.BaseDirectory + ConfigurationManager.AppSettings["plugindirectory"];

            //Connection strings
            CrmConnection = ConfigurationManager.ConnectionStrings["CRM"].ConnectionString;
            MailWhConnection = ConfigurationManager.ConnectionStrings["MailWH"].ConnectionString;
        }

    }
}
