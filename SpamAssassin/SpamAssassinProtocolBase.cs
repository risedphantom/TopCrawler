using System;
using System.Net.Sockets;
using System.Text;

namespace SpamAssassin
{
    /// <summary>
    /// Implements an easy-to-use way to communicate with a SpamAssassin server.
    /// </summary>
    public class SpamAssassinProtocolBase
    {
        #region --- Constructors ---
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="serverName">The server to connect to</param>
        /// <param name="serverPort">The port of the server to connect to. The default value is 783</param>
        public SpamAssassinProtocolBase(string serverName, int serverPort)
        {
            ServerName = serverName;
            ServerPort = serverPort;
        }

        /// <summary>
        /// Constructor. Uses the default port 783
        /// </summary>
        /// <param name="serverName">The server to connect to</param>
        public SpamAssassinProtocolBase(string serverName)
        {
            ServerName = serverName;
            ServerPort = 783;
        }
        #endregion

        #region --- Public properties ---
        /// <summary>
        /// The version of the SPAMC client that this class implements
        /// </summary>
        public readonly string SpamCVersion = "1.2";

        public string UserName { get; set; }
        public string ServerName { get; private set; }
        public int ServerPort { get; private set; }

        #endregion

        #region --- Protected methods ---
        /// <summary>
        /// High-level function for sending a message to the server and receiving the response message.
        /// </summary>
        /// <param name="command">The command to send</param>
        /// <param name="message">The request message to send</param>
        /// <returns>Returns the received response message</returns>
        protected string SendMessage(string command, string message)
        {
            return SendMessageEx(command, message).Message;
        }

        /// <summary>
        /// High-level function for sending a message to the server and receiving the response message
        /// </summary>
        /// <param name="command">The command to send</param>
        /// <param name="message">The request message to send</param>
        /// <returns>Returns the received response message</returns>
        protected ResponsePacket SendMessageEx(string command, string message)
        {
            return SendRequest(new RequestPacket(this, command, message));
        }
        #endregion
        
        #region --- Private methods ---
        /// <summary>
        /// Central function for sending a request and reading the response
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private ResponsePacket SendRequest(RequestPacket request)
        {
            using (var spamAssassinSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                spamAssassinSocket.Connect(ServerName, ServerPort);

                var messageBuffer = Encoding.ASCII.GetBytes(request.RawPacket);

                spamAssassinSocket.Send(messageBuffer);
                spamAssassinSocket.Shutdown(SocketShutdown.Send);

                int received;
                var receivedMessage = string.Empty;
                do
                {
                    var receiveBuffer = new byte[1024];
                    received = spamAssassinSocket.Receive(receiveBuffer);
            
                    receivedMessage += Encoding.ASCII.GetString(receiveBuffer, 0, received);
                }
                while (received > 0);

                spamAssassinSocket.Shutdown(SocketShutdown.Both);

                if (request.Command != SpamAssassinCommands.Skip && !receivedMessage.StartsWith("SPAMD", StringComparison.InvariantCultureIgnoreCase))
                {
                    //TODO: Something
                }

                return new ResponsePacket(this, request, receivedMessage.Trim());
            }
        }

        #endregion

        #region -- Protected base class for one packet ---
        /// <summary>
        /// Base class for request and response packets
        /// </summary>
        protected class PacketBase
        {
            #region --- Constructors ---

            public PacketBase(SpamAssassinProtocolBase owner)
            {
                Owner = owner;
            }

            #endregion

            #region --- Properties ---

            protected SpamAssassinProtocolBase Owner { get; private set; }
            public string RawPacket { get; set; }

            #endregion
        }

        // ------------------------------------------------------------------
        #endregion

        #region --- Protected class for a packet to send ---
        /// <summary>
        /// A request, initiated from SPAMC to SPAMD
        /// </summary>
        protected class RequestPacket : PacketBase
        {
            #region --- Constructors ---

            public RequestPacket(SpamAssassinProtocolBase owner, string command, string message) : base(owner)
            {
                Command = command;
                Message = message;

                RawPacket = BuildRawPacket(command, message);
            }

            #endregion

            #region --- Properties ---

            public string Command { get; private set; }
            public string Message { get; private set; }

            #endregion

            #region --- Private methods ---

            private string BuildRawPacket(string command, string message)
            {
                var sb = new StringBuilder();

                sb.AppendFormat("{0} SPAMC/{1}\r\n", command, Owner.SpamCVersion);

                if (!String.IsNullOrEmpty(Owner.UserName))
                    sb.AppendFormat("User: {0}\r\n", Owner.UserName);
                
                sb.AppendFormat("Content-Length: {0}\r\n\r\n", message.Length);
                sb.AppendFormat(message);

                return sb.ToString();
            }

            #endregion
        }
        #endregion

        #region --- Protected class for a received package ---
        /// <summary>
        /// A response, returned from SPAMD to SPAMC
        /// </summary>
        protected class ResponsePacket : PacketBase
        {
            #region --- Constructors ---

            public ResponsePacket(SpamAssassinProtocolBase owner, RequestPacket associatedRequestPackage, string rawPacket) : base(owner)
            {
                RawPacket = rawPacket;
                AssociatedRequestPackage = associatedRequestPackage;

                ParseRawPacket(rawPacket);
            }

            #endregion

            #region --- Properties ---

            public string[] RawLines { get; private set; }
            public string Message { get; private set; }
            public string ProtocolVersion { get; private set; }
            public string ResponseMessage { get; private set; }
            public int ResponseCode { get; private set; }
            public RequestPacket AssociatedRequestPackage { get; private set; }
            
            #endregion

            #region --- Private methods ---

            private void ParseRawPacket(string rawPacket)
            {
                if (string.IsNullOrEmpty(rawPacket)) 
                    return;

                rawPacket = rawPacket.Replace("\r\n", "\n");
                rawPacket = rawPacket.Replace("\r", "\n");

                RawLines = rawPacket.Split('\n');

                var line1Elements = RawLines[0].Split(' ');

                ProtocolVersion = line1Elements[0].Replace("SPAMD/", string.Empty);
                ResponseCode = Convert.ToInt32(line1Elements[1]);

                ResponseMessage = string.Empty;
                for (var i = 2; i < line1Elements.Length; ++i)
                    ResponseMessage += " " + line1Elements[i];
                
                ResponseMessage = ResponseMessage.Trim();

                var line1Plus = String.Join("\r\n", RawLines, 1, RawLines.Length - 1);
                Message = line1Plus.Trim();
            }

            #endregion
        }
        #endregion
    }

}
