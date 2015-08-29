using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using OpenPop.Mime;
using OpenPop.Pop3;
using Interfaces;

namespace TopCrawler
{
    #region --- Types ---
    enum MessageType
    {
        GENERAL = 100,
        MISSION = 200,
        BOUNCE = 300,
        FBL = 400,
        UNKNOWN = 900,
    }

    class MessageInfo
    {
        public bool IsSpam { get; set; }
        public Message Mail { get; set; }
        public string Subtype { get; set; }
        public string Recipient { get; set; }
        public MessageType Type { get; set; }
        public MailboxInfo Mailbox { get; set; }
    }
    #endregion

    class Crawler
    {
        private readonly Config _config;
        private volatile bool _stopPipeline;

        //Pipeline
        private TransformBlock<MessageInfo, MessageInfo> _sortMailDataBlock;
        private TransformBlock<MessageInfo, MessageInfo> _spamFilterDataBlock;
        private TransformBlock<MessageInfo, MessageInfo> _checkBounceDataBlock;
        private TransformBlock<MessageInfo, MessageInfo> _identifyDataBlock;
        private ActionBlock<MessageInfo> _addToCrmDataBlock;
        private ActionBlock<MessageInfo> _addToFblDataBlock;
        private ActionBlock<MessageInfo> _addToBounceDataBlock;

        //Plugins
        [ImportMany]
        public IEnumerable<Lazy<ICondition, IConditionMetadata>> BounceTypeConditions { get; set; }

        [ImportMany]
        public IEnumerable<Lazy<IIdentification, IIdentificationMetadata>> IdentifyRules { get; set; }

        private void LoadPlugins()
        {
            try
            {
                var container = new CompositionContainer(new DirectoryCatalog(_config.PluginDirectory), true);
                container.ComposeParts(this);
            }
            catch (Exception ex)
            {
                Functions.Log.Error("Unable to load plugins: {0}", ex.Message);
            }
        }

        public Crawler(Config config)
        {
            _config = config;
            LoadPlugins();
        }

        public void Init()
        {
            //*** Create pipeline ***
            //Create TransformBlock to get message type
            var blockOptions = _config.GetDataBlockOptions("_sortMailDataBlock");
            _sortMailDataBlock = new TransformBlock<MessageInfo, MessageInfo>(mail => SortMail(mail),
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = blockOptions.Maxdop,
                    BoundedCapacity = blockOptions.BoundedCapacity
                });
            //Create TransformBlock to filter spam
            blockOptions = _config.GetDataBlockOptions("_spamFilterDataBlock");
            _spamFilterDataBlock = new TransformBlock<MessageInfo, MessageInfo>(mail => FilterSpam(mail),
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = blockOptions.Maxdop,
                    BoundedCapacity = blockOptions.BoundedCapacity
                });
            //Create TransformBlock to sort bounces
            blockOptions = _config.GetDataBlockOptions("_checkBounceDataBlock");
            _checkBounceDataBlock = new TransformBlock<MessageInfo, MessageInfo>(mail => BounceTypeCheck(mail),
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = blockOptions.Maxdop,
                    BoundedCapacity = blockOptions.BoundedCapacity
                });
            //Create TransformBlock to identify bounce owner
            blockOptions = _config.GetDataBlockOptions("_identifyDataBlock");
            _identifyDataBlock = new TransformBlock<MessageInfo, MessageInfo>(mail => GetRecipient(mail),
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = blockOptions.Maxdop,
                    BoundedCapacity = blockOptions.BoundedCapacity
                });
            //Create ActionBlock to send mail to CRM
            blockOptions = _config.GetDataBlockOptions("_addToCrmDataBlock");
            _addToCrmDataBlock = new ActionBlock<MessageInfo>(mail => AddToCrm(mail),
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = blockOptions.Maxdop,
                    BoundedCapacity = blockOptions.BoundedCapacity
                });
            //Create ActionBlock to send FBL to MailWH
            blockOptions = _config.GetDataBlockOptions("_addToFblDataBlock");
            _addToFblDataBlock = new ActionBlock<MessageInfo>(mail => AddToFbl(mail),
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = blockOptions.Maxdop,
                    BoundedCapacity = blockOptions.BoundedCapacity
                });
            //Create ActionBlock to send Bounce to MailWH
            blockOptions = _config.GetDataBlockOptions("_addToBounceDataBlock");
            _addToBounceDataBlock = new ActionBlock<MessageInfo>(mail => AddToBounce(mail),
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = blockOptions.Maxdop,
                    BoundedCapacity = blockOptions.BoundedCapacity
                });

            //*** Build pipeline ***
            _sortMailDataBlock.LinkTo(_spamFilterDataBlock, info => info.Type == MessageType.GENERAL);
            _sortMailDataBlock.LinkTo(_addToFblDataBlock, info => info.Type == MessageType.FBL);
            _sortMailDataBlock.LinkTo(_checkBounceDataBlock, info => info.Type == MessageType.BOUNCE);
            _sortMailDataBlock.LinkTo(DataflowBlock.NullTarget<MessageInfo>(), info => info.Type == MessageType.UNKNOWN); /*STUB*/
            _checkBounceDataBlock.LinkTo(_identifyDataBlock);
            _identifyDataBlock.LinkTo(_addToBounceDataBlock);
            _spamFilterDataBlock.LinkTo(_addToCrmDataBlock, info => !info.IsSpam);
            _spamFilterDataBlock.LinkTo(DataflowBlock.NullTarget<MessageInfo>(), info => info.IsSpam); /*STUB*/

            _sortMailDataBlock.Completion.ContinueWith(t =>
            {
                if (t.IsFaulted) ((IDataflowBlock)_spamFilterDataBlock).Fault(t.Exception);
                else _spamFilterDataBlock.Complete();
            });
            _sortMailDataBlock.Completion.ContinueWith(t =>
            {
                if (t.IsFaulted) ((IDataflowBlock)_addToFblDataBlock).Fault(t.Exception);
                else _addToFblDataBlock.Complete();
            });
            _sortMailDataBlock.Completion.ContinueWith(t =>
            {
                if (t.IsFaulted) ((IDataflowBlock)_checkBounceDataBlock).Fault(t.Exception);
                else _checkBounceDataBlock.Complete();
            });
            _spamFilterDataBlock.Completion.ContinueWith(t =>
            {
                if (t.IsFaulted) ((IDataflowBlock)_addToCrmDataBlock).Fault(t.Exception);
                else _addToCrmDataBlock.Complete();
            });
            _checkBounceDataBlock.Completion.ContinueWith(t =>
            {
                if (t.IsFaulted) ((IDataflowBlock)_identifyDataBlock).Fault(t.Exception);
                else _identifyDataBlock.Complete();
            });
            _identifyDataBlock.Completion.ContinueWith(t =>
            {
                if (t.IsFaulted) ((IDataflowBlock)_addToBounceDataBlock).Fault(t.Exception);
                else _addToBounceDataBlock.Complete();
            });
        }

        public void Start()
        {
            do
            {
                var getMailsTasks = _config.CredentialsList.Select(credentials => Task.Run(() => GetMails(credentials))).ToList();

                foreach (var task in getMailsTasks)
                    task.Wait();

                Thread.Sleep(2000);
            } while (!_stopPipeline);

            //Stop pipeline - wait for completion of all endpoints
            _sortMailDataBlock.Complete();
            _addToCrmDataBlock.Completion.Wait();
            _addToFblDataBlock.Completion.Wait();
            _addToBounceDataBlock.Completion.Wait();
            if (_stopPipeline)
                Functions.Log.Warn("Pipeline has been stopped by user");
        }

        public void Stop()
        {
            _stopPipeline = true;
        }

        private void GetMails(MailboxInfo info)
        {
            try
            {
                using (var client = new Pop3Client())
                {
                    //Get Zabbix metrics
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    //Get mail count
                    client.Connect(info.Hostname, info.Port, false);
                    client.Authenticate(info.User, info.Password);
                    stopwatch.Stop();

                    //Send it to Zabbix
                    Functions.Zabbix.SendData(new ZabbixItem { Host = _config.HostKey, Key = info.Type + _config.TimingKey, Value = stopwatch.ElapsedMilliseconds.ToString() });
                    Functions.Log.Debug("Send [{0}] timing to Zabbix: connected to '{1}' as '{2}', timing {3}ms", info.Type, info.Hostname, info.User, stopwatch.ElapsedMilliseconds);

                    var count = client.GetMessageCount();
                    if (count == 0)
                        return;

                    Functions.Log.Debug("We've got new {0} messages in '{1}'", count, info.User);
                    //Send messages to sorting block
                    for (var i = 0; i < count; i++)
                    {
                        try
                        {
                            var mailInfo = new MessageInfo
                            {
                                IsSpam = false,
                                Mail = client.GetMessage(i + 1),
                                Type = MessageType.UNKNOWN,
                                Subtype = null,
                                Recipient = null,
                                Mailbox = info
                            };
                            Functions.Log.Debug("Download message from '{0}'. Size: {1}b", info.User, mailInfo.Mail.RawMessage.Length);
                            while (!_sortMailDataBlock.Post(mailInfo))
                                Thread.Sleep(500);

                            //Save every mail to archive
                            Functions.Log.Debug("Archive message");
                            Functions.Archive.Info(Functions.MessageToString(mailInfo.Mail));
                        }
                        catch (Exception ex)
                        {
                            Functions.Log.Error("Parse email error: {0}", ex.Message);
                            Functions.ErrorsCounters[info.Type].Increment();

                            //Archive mail anyway
                            Functions.Log.Debug("Archive message");
                            Functions.Archive.Info(Encoding.Default.GetString(client.GetMessageAsBytes(i + 1)));
                        }

                        if (_config.DeleteMail)
                            client.DeleteMessage(i + 1);

                        if (_stopPipeline)
                            break;
                    }
                    Functions.Log.Debug("Done with '{0}'", info.User);
                }
            }
            catch (Exception ex)
            {
                Functions.Log.Error("General error - type: {0}, message: {1}", ex, ex.Message);
                Functions.ErrorsCounters[info.Type].Increment();
            }
        }

        #region --- Pipeline methods ---

        private MessageInfo SortMail(MessageInfo mail)
        {
            switch (mail.Mailbox.Type)
            {
                case MailboxType.Crm:
                    mail.Type = MessageType.GENERAL;
                    break;
                case MailboxType.Bounce:
                    mail.Type = MessageType.BOUNCE;
                    break;
                case MailboxType.Fbl:
                    mail.Type = MessageType.FBL;
                    break;
            }
            return mail;
        }

        private MessageInfo FilterSpam(MessageInfo mail)
        {
            //TODO: Add SpamAssassin logic
            return mail;
        }

        private MessageInfo BounceTypeCheck(MessageInfo mailInfo)
        {
            try
            {
                foreach (var condition in BounceTypeConditions)
                {
                    var res = condition.Value.Check(mailInfo.Mail);
                    if (res == null)
                        continue;

                    mailInfo.Subtype = res;
                    Functions.Log.Debug("Bounce type condition [{0}] triggered for message [{1}]", condition.Metadata.Type, mailInfo.Mail.Headers.MessageId);
                    break;
                }
            }
            catch (Exception ex)
            {
                Functions.Log.Error("Failed to determine bounce type for message '{0}': {1}", mailInfo.Mail.Headers.MessageId, ex.Message);
                Functions.ErrorsCounters[MailboxType.Bounce].Increment();
            }
            return mailInfo;
        }

        private MessageInfo GetRecipient(MessageInfo mailInfo)
        {
            try
            {
                foreach (var rule in IdentifyRules)
                {
                    var res = rule.Value.FindRecipient(mailInfo.Mail);
                    if (res == null)
                        continue;

                    mailInfo.Recipient = res;
                    Functions.Log.Debug("Bounce identification rule [{0}] triggered for message [{1}]", rule.Metadata.Type, mailInfo.Mail.Headers.MessageId);
                    break;
                }
            }
            catch (Exception ex)
            {
                Functions.Log.Error("Failed to identify bounce source for message '{0}': {1}", mailInfo.Mail.Headers.MessageId, ex.Message);
                Functions.ErrorsCounters[MailboxType.Bounce].Increment();
            }
            return mailInfo;
        }

        private void AddToCrm(MessageInfo mail)
        {
            try
            {
                Crm.SaveEntireMessage(mail);
                Functions.ProcessedCounters[MailboxType.Crm].Increment();
                Functions.Log.Debug("Send to CRM");
            }
            catch (Exception ex)
            {
                Functions.Log.Error("Error saving message '{0}' to CRM: {1}", mail.Mail.Headers.MessageId, ex.Message);
                Functions.ErrorsCounters[MailboxType.Crm].Increment();
            }
        }

        private void AddToFbl(MessageInfo mail)
        {
            try
            {
                // TODO: Add FBL additional headers support
                MailWH.FBLAdd(mail);
                Functions.ProcessedCounters[MailboxType.Fbl].Increment();
                Functions.Log.Debug("Send FBL to MailWH");
            }
            catch (Exception ex)
            {
                Functions.Log.Error("Error saving FBL message '{0}' to MailWH: {1}", mail.Mail.Headers.MessageId, ex.Message);
                Functions.ErrorsCounters[MailboxType.Fbl].Increment();
            }        
        }

        private void AddToBounce(MessageInfo mail)
        {
            try
            {
                MailWH.BounceAdd(mail);
                Functions.ProcessedCounters[MailboxType.Bounce].Increment();
                Functions.Log.Debug("Send Bounce to MailWH");
            }
            catch (Exception ex)
            {
                Functions.Log.Error("Error saving Bounce message '{0}' to MailWH: {1}", mail.Mail.Headers.MessageId, ex.Message);
                Functions.ErrorsCounters[MailboxType.Bounce].Increment();
            }
        }

        #endregion
    }
}
