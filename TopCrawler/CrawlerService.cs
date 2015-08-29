using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Threading;

namespace TopCrawler
{
    // Custom commands
    public enum CustomCommand
    {
        DELICATE_STOP = 0x00000080,
        CHECK_WORKER  = 0x00000081
    }

    public class CrawlerService : ServiceBase
    {
        private static readonly ManualResetEvent Pause = new ManualResetEvent(false);
        private static readonly Dictionary<string, long> LastProcessedCount = new Dictionary<string, long>();
        private static readonly Dictionary<string, long> LastErrorsCount = new Dictionary<string, long>();
        private static Config _config;
        private static Crawler _crawler;
        private Thread _workerThread;

        static void Main()
        {
            var services = new ServiceBase[]
			{
				new CrawlerService()
			};

            Run(services);
        }

        // Start the service
        protected override void OnStart(string[] args)
        {
            try
            {
                Functions.Log.Info("-=START=-");
                _workerThread = new Thread(Process);
                _workerThread.Start();

                if (_workerThread != null)
                    Functions.Log.Debug("Worker thread state = {0}", _workerThread.ThreadState.ToString());
            }
            catch (Exception ex)
            {
                Functions.Log.Error("Error on start: {0}", ex.Message);
            }
        }

        // Stop the service
        protected override void OnStop()
        {
            Functions.Log.Info("-=STOP=-");
            if ((_workerThread != null) && (_workerThread.IsAlive))
            {
                Pause.Reset();
                Thread.Sleep(1000);
                _workerThread.Abort();
            }
            ExitCode = 0;
        }

        //Process custom command
        protected override void OnCustomCommand(int command)
        {
            switch (command)
            {
                case (int)CustomCommand.DELICATE_STOP:
                    Functions.Log.Info("-=DELICATE STOP=-");
                    RequestStop();

                    while (_workerThread != null && _workerThread.IsAlive)
                        Thread.Sleep(100);

                    Stop();
                    break;
                case (int)CustomCommand.CHECK_WORKER:
                    break;
            }
        }

        #region --- Methods ---
        private static void Process()
        {
            //Read configuration
            _config = new Config();
            _config.Read();

            //Init
            Functions.Zabbix = new ZabbixSender(_config.ZabbixServer, _config.ZabbixPort);
            Crm.ConnectionString = _config.CrmConnection;
            MailWH.ConnectionString = _config.MailWhConnection;
            var timer = new Timer(GetStatus, null, 0, _config.NotifyPeriod * 1000);

            //Create monitoring counters
            Functions.ProcessedCounters.Add(MailboxType.Crm, new Counter());
            Functions.ProcessedCounters.Add(MailboxType.Fbl, new Counter());
            Functions.ProcessedCounters.Add(MailboxType.Bounce, new Counter());
            Functions.ErrorsCounters.Add(MailboxType.Crm, new Counter());
            Functions.ErrorsCounters.Add(MailboxType.Fbl, new Counter());
            Functions.ErrorsCounters.Add(MailboxType.Bounce, new Counter());
            LastProcessedCount.Add(MailboxType.Crm, 0);
            LastProcessedCount.Add(MailboxType.Fbl, 0);
            LastProcessedCount.Add(MailboxType.Bounce, 0);
            LastErrorsCount.Add(MailboxType.Crm, 0);
            LastErrorsCount.Add(MailboxType.Fbl, 0);
            LastErrorsCount.Add(MailboxType.Bounce, 0);
            
            //Process
            _crawler = new Crawler(_config);
            _crawler.Init();
            _crawler.Start();

            timer.Dispose();
        }

        private static void GetStatus(object sender)
        {
            //Notify about processed
            foreach (var counter in Functions.ProcessedCounters)
            {
                var curCount = counter.Value.Read();
                Functions.Zabbix.SendData(new ZabbixItem { Host = _config.HostKey, Key = counter.Key + _config.ProcessedCountKey, Value = (curCount - LastProcessedCount[counter.Key]).ToString() });
                Functions.Log.Debug("Send to zabbix - [{0}] processed: {1}", counter.Key, curCount - LastProcessedCount[counter.Key]);
                Functions.Log.Debug("Total - [{0}] processed: {1}", counter.Key, curCount);
                LastProcessedCount[counter.Key] = curCount;
            }
            //Notify about errors
            foreach (var counter in Functions.ErrorsCounters)
            {
                var curCount = counter.Value.Read();
                Functions.Zabbix.SendData(new ZabbixItem { Host = _config.HostKey, Key = counter.Key + _config.ErrorKey, Value = (curCount - LastErrorsCount[counter.Key]).ToString() });
                Functions.Log.Debug("Send to zabbix - [{0}] errors: {1}", counter.Key, curCount - LastErrorsCount[counter.Key]);
                Functions.Log.Debug("Total - [{0}] errors: {1}", counter.Key, curCount);
                LastErrorsCount[counter.Key] = curCount;
            }
        }

        public void RequestStop()
        {
            if (_crawler != null)
                _crawler.Stop();
        }
        #endregion
    }
}
