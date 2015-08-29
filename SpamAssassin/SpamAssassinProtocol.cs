using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;

namespace SpamAssassin
{
    /// <summary>
    /// Implements an easy-to-use way to communicate with a SpamAssassin server
    /// </summary>
    public sealed class SpamAssassinProtocol : SpamAssassinProtocolBase
    {
        #region --- Constructors ---
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="serverName">The server to connect to</param>
        /// <param name="serverPort">The port of the server to connect to. The default value is 783</param>
        public SpamAssassinProtocol(string serverName, int serverPort) : base(serverName, serverPort)
        {
        }

        /// <summary>
        /// Constructor. Uses the default port 783
        /// </summary>
        /// <param name="serverName">The server to connect to</param>
        public SpamAssassinProtocol(string serverName) : base(serverName)
        {
        }
        #endregion

        #region --- Commands ---
        /// <summary>
        /// Execute the Check command
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public SpamAssassinCheckResult ExecuteCheck(SpamAssassinCheckArgs e)
        {
            string[] additionalLines;
            var result = new SpamAssassinCheckResult();
            var responsePacket = SendMessageEx(SpamAssassinCommands.Check, PrepareCheckRequestMessage(e));
            
            CheckThrowResponsePacket(responsePacket);
            InterpretCheckResponseMessage(responsePacket.Message, out additionalLines, result);

            return result;
        }

        /// <summary>
        /// Execute the Symbols command
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public SpamAssassinSymbolsResult ExecuteSymbols(SpamAssassinSymbolsArgs e)
        {
            string[] additionalLines;
            var result = new SpamAssassinSymbolsResult();
            var responsePacket = SendMessageEx(SpamAssassinCommands.Symbols, PrepareCheckRequestMessage(e));
            
            CheckThrowResponsePacket(responsePacket);
            InterpretCheckResponseMessage(responsePacket.Message, out additionalLines, result);

            // Remove empty line at the beginning.
            additionalLines = SplitLines(JoinLines(additionalLines));

            result.SymbolLines = additionalLines[0].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            return result;
        }

        /// <summary>
        /// Execute the Report command
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public SpamAssassinReportResult ExecuteReport(SpamAssassinReportArgs e)
        {
            string[] additionalLines;
            var result = new SpamAssassinReportResult();
            var responsePacket = SendMessageEx(SpamAssassinCommands.Report, PrepareCheckRequestMessage(e));
            
            CheckThrowResponsePacket(responsePacket);
            InterpretCheckResponseMessage(responsePacket.Message, out additionalLines, result);

            result.ReportText = JoinLines(additionalLines);

            return result;
        }

        /// <summary>
        /// Execute the ReportIfSpam command
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public SpamAssassinReportIfSpamResult ExecuteReportIfSpam(SpamAssassinReportIfSpamArgs e)
        {
            string[] additionalLines;
            var result = new SpamAssassinReportIfSpamResult();
            var responsePacket = SendMessageEx(SpamAssassinCommands.ReportIfSpam, PrepareCheckRequestMessage(e));
            
            CheckThrowResponsePacket(responsePacket);
            InterpretCheckResponseMessage(responsePacket.Message, out additionalLines, result);

            result.ReportText = JoinLines(additionalLines);
            
            return result;
        }

        /// <summary>
        /// Execute the Skip command
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public SpamAssassinSkipResult ExecuteSkip(SpamAssassinSkipArgs e)
        {
            var responsePacket = SendMessageEx(SpamAssassinCommands.Skip, string.Empty);
            CheckThrowResponsePacket(responsePacket);

            return new SpamAssassinSkipResult();
        }

        /// <summary>
        /// Execute the Ping command
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public SpamAssassinPingResult ExecutePing(SpamAssassinPingArgs e)
        {
            var responsePacket = SendMessageEx(SpamAssassinCommands.Ping, string.Empty);
            CheckThrowResponsePacket(responsePacket);

            if (String.Compare(responsePacket.ResponseMessage, "PONG", true) != 0)
                throw new SpamAssassinException(String.Format("The PING response from SPAMD was '{0}' but is expected to be 'PONG'", responsePacket.ResponseMessage));
            
            return new SpamAssassinPingResult();
        }

        /// <summary>
        /// Execute the Process command
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public SpamAssassinProcessResult ExecuteProcess(SpamAssassinProcessArgs e)
        {
            var result = new SpamAssassinProcessResult();
            var responsePacket = SendMessageEx(SpamAssassinCommands.Process, PrepareCheckRequestMessage(e));
            CheckThrowResponsePacket(responsePacket);

            var lines = SplitLines(responsePacket.Message);
            lines = RemoveLine(lines, 0);

            result.ProcessedMessage = JoinLines(lines);

            return result;
        }

        /// <summary>
        /// Execute the Tell command
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public SpamAssassinTellResult ExecuteTell(SpamAssassinTellArgs e)
        {
            const string messageClassString = "spam";
            var setString = String.Empty;
            var removeString = String.Empty;

            if ((e.SetLocation & SpamAssassinTellArgs.Location.LOCAL) != 0)
                setString += "local";
            else if ((e.SetLocation & SpamAssassinTellArgs.Location.REMOTE) != 0)
            {
                if (!String.IsNullOrEmpty(setString))
                    setString += ", ";
                setString += "remote";
            }

            if ((e.RemoveLocation & SpamAssassinTellArgs.Location.LOCAL) != 0)
                removeString += "local";
            else if ((e.RemoveLocation & SpamAssassinTellArgs.Location.REMOTE) != 0)
            {
                if (!String.IsNullOrEmpty(removeString))
                    removeString += ", ";
                removeString += "remote";
            }

            var sb = new StringBuilder();

            sb.AppendLine(String.Format("Message-class: {0}", messageClassString));
            if (!String.IsNullOrEmpty(setString))
                sb.AppendLine(String.Format("Set: {0}", setString));
            if (!String.IsNullOrEmpty(removeString))
                sb.AppendLine(String.Format("Remove: {0}", removeString));
            
            var requestMessage = PrepareCheckRequestMessage(e);
            sb.AppendLine();
            sb.AppendLine(requestMessage);

            var responsePacket = SendMessageEx(SpamAssassinCommands.Tell, sb.ToString());
            CheckThrowResponsePacket(responsePacket);

            return new SpamAssassinTellResult
            {
                DidSet = responsePacket.Message.IndexOf("DidSet", StringComparison.InvariantCultureIgnoreCase) >= 0,
                DidRemove = responsePacket.Message.IndexOf("DidRemove", StringComparison.InvariantCultureIgnoreCase) >= 0
            };
        }
        #endregion

        #region --- Private methods ---
        /// <summary>
        /// Internal helper
        /// </summary>
        private static string PrepareCheckRequestMessage(SpamAssassinCheckArgs e)
        {
            // Create mini-RFC822 message and translate it to DOS format for spamd.
            var requestMessage =
@"From {SenderEMailAddress} {EMailDateUTC}
Received: from {SenderHostName} ({SenderHostAddress}) by {ServerHostName} with HTTP via ZetaSoftware;
	{EMailDateRFC2822}
From: {SenderEMailName} <{ReceiverEMailAddress}>
Date: {EMailDateRFC2822}
Subject: ZetaSoftware comment
To: {ReceiverEMailAddress}

{TextToCheck}";

            requestMessage = requestMessage.Replace("{SenderEMailName}", e.SenderEMailName);
            requestMessage = requestMessage.Replace("{SenderEMailAddress}", e.SenderEMailAddress);
            requestMessage = requestMessage.Replace("{EMailDateUTC}", e.EMailDate.ToUniversalTime().ToString("ddd MMM dd HH:mm:ss yyyy", CultureInfo.InvariantCulture));
            requestMessage = requestMessage.Replace("{SenderHostName}", e.SenderHostName);
            requestMessage = requestMessage.Replace("{SenderHostAddress}", e.SenderHostAddress);
            requestMessage = requestMessage.Replace("{ServerHostName}", e.ServerHostName);
            requestMessage = requestMessage.Replace("{EMailDateRFC2822}", e.EMailDate.ToString("r"));
            requestMessage = requestMessage.Replace("{ReceiverEMailAddress}", e.ReceiverEMailAddress);
            requestMessage = requestMessage.Replace("{TextToCheck}", e.TextToCheck);

            return requestMessage;
        }


        /// <summary>
        /// Internal helper
        /// </summary>
        private void InterpretCheckResponseMessage(string responseMessage, out string[] additionalLines, SpamAssassinCheckResult result)
        {
            var lines = SplitLines(responseMessage);
            var firstLineColumns = lines[0].Split(' ');

            var spamdFlag = firstLineColumns[1];
            var spamdScore = firstLineColumns[3];
            var spamdThreshold = firstLineColumns[5];

            result.IsSpam = String.Compare(spamdFlag, "True", true) == 0;
            result.Score = Convert.ToDouble(spamdScore, CultureInfo.InvariantCulture);
            result.Threshold = Convert.ToDouble(spamdThreshold, CultureInfo.InvariantCulture);

            var rawAdditionalLines = new ArrayList();

            for (var index = 1; index < lines.Length; index++)
                rawAdditionalLines.Add(lines[index]);
            
            additionalLines = (string[])rawAdditionalLines.ToArray(typeof(string));
        }

        /// <summary>
        /// Line-operation helper
        /// </summary>
        private static string JoinLines(string[] lines)
        {
            if (lines == null || lines.Length <= 0)
                return String.Empty;
            
            return String.Join("\r\n", lines).Trim();
        }

        /// <summary>
        /// Line-operation helper
        /// </summary>
        private static string[] SplitLines(string text)
        {
            return String.IsNullOrEmpty(text) ? new string[] {} : text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        }

        /// <summary>
        /// Line-operation helper
        /// </summary>
        private static string[] RemoveLine(string[] lines, int indexToRemove)
        {
            if (lines == null || lines.Length <= indexToRemove)
                return lines;
            
            var list = new ArrayList(lines);
            list.RemoveAt(indexToRemove);

            if (list.Count <= 0)
                return new string[] { };
            
            return (string[])list.ToArray(typeof(string));
        }

        /// <summary>
        /// Throws an exception if a code!=0 is returned
        /// </summary>
        private static void CheckThrowResponsePacket(ResponsePacket responsePacket)
        {
            if (responsePacket.ResponseCode != 0)
                throw new SpamAssassinException(String.Format("{0} ({1})", responsePacket.ResponseMessage, responsePacket.ResponseCode));
        }
        #endregion
    }

    /////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Parameters to the execute function
    /// </summary>
    public class SpamAssassinCheckArgs
    {
        public DateTime EMailDate = DateTime.Now;
        public string SenderEMailName = "Travel crawler";
        public string SenderEMailAddress = "customerservice@ozon.travel";
        public string SenderHostName = Dns.GetHostName();
        public string SenderHostAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
        public string ServerHostName = Dns.GetHostName();
        public string ReceiverEMailAddress = "customerservice@ozon.travel";
        public string TextToCheck;
    }

    /// <summary>
    /// Result from the execute function
    /// </summary>
    public class SpamAssassinCheckResult
    {
        public bool IsSpam;
        public double Score;
        public double Threshold;
    }

    /// <summary>
    /// Parameters to the execute function
    /// </summary>
    public class SpamAssassinSymbolsArgs : SpamAssassinCheckArgs
    {
    }

    /// <summary>
    /// Result from the execute function
    /// </summary>
    public class SpamAssassinSymbolsResult : SpamAssassinCheckResult
    {
        public string[] SymbolLines;
    }

    /// <summary>
    /// Parameters to the execute function
    /// </summary>
    public class SpamAssassinReportArgs : SpamAssassinCheckArgs
    {
    }

    /// <summary>
    /// Result from the execute function
    /// </summary>
    public class SpamAssassinReportResult : SpamAssassinCheckResult
    {
        public string ReportText;
    }

    /// <summary>
    /// Parameters to the execute function
    /// </summary>
    public class SpamAssassinReportIfSpamArgs : SpamAssassinCheckArgs
    {
    }

    /// <summary>
    /// Result from the execute function
    /// </summary>
    public class SpamAssassinReportIfSpamResult : SpamAssassinCheckResult
    {
        public string ReportText;
    }

    /// <summary>
    /// Parameters to the execute function
    /// </summary>
    public class SpamAssassinSkipArgs
    {
    }

    /// <summary>
    /// Result from the execute function
    /// </summary>
    public class SpamAssassinSkipResult
    {
    }

    /// <summary>
    /// Parameters to the execute function
    /// </summary>
    public class SpamAssassinPingArgs
    {
    }

    /// <summary>
    /// Result from the execute function
    /// </summary>
    public class SpamAssassinPingResult
    {
    }

    /// <summary>
    /// Parameters to the execute function
    /// </summary>
    public class SpamAssassinProcessArgs : SpamAssassinCheckArgs
    {
    }

    /// <summary>
    /// Result from the execute function
    /// </summary>
    public class SpamAssassinProcessResult
    {
        public string ProcessedMessage;
    }

    /// <summary>
    /// Parameters to the execute function
    /// </summary>
    public class SpamAssassinTellArgs : SpamAssassinCheckArgs
    {
        #region Public variables.
        /// <summary>
        /// High-level interface to the Location enumeration
        /// </summary>
        public enum TellAction
        {
            LEARN_MESSAGE_AS_SPAM,
            FORGET_LEARNED_MESSAGE,
            REPORT_SPAM_MESSAGE,
            REVOKE_HAM_MESSAGE
        }

        [Flags]
        public enum Location
        {
            LOCAL = 0x01,
            REMOTE = 0x02
        }

        /// <summary>
        /// High-level interface to the Location enumeration
        /// </summary>
        public TellAction Action
        {
            set
            {
                switch (value)
                {
                    case TellAction.LEARN_MESSAGE_AS_SPAM:
                        SetLocation = Location.LOCAL;
                        RemoveLocation = 0;
                        break;
                    case TellAction.FORGET_LEARNED_MESSAGE:
                        SetLocation = 0;
                        RemoveLocation = Location.LOCAL;
                        break;
                    case TellAction.REPORT_SPAM_MESSAGE:
                        SetLocation = Location.LOCAL | Location.REMOTE;
                        RemoveLocation = 0;
                        break;
                    case TellAction.REVOKE_HAM_MESSAGE:
                        SetLocation = Location.LOCAL;
                        RemoveLocation = Location.REMOTE;
                        break;
                    default:
                        Debug.Assert(false, String.Format("Unknown TellAction '{0}'.", value));
                        break;
                }
            }
        }

        public Location SetLocation;
        public Location RemoveLocation;

        #endregion
    }

    /// <summary>
    /// Result from the execute function
    /// </summary>
    public class SpamAssassinTellResult
    {
        public bool DidSet;
        public bool DidRemove;
    }
}
