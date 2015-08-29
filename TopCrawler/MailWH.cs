using System;
using System.Data;
using System.Data.SqlClient;

namespace TopCrawler
{
    /// <summary>
    /// Singleton: class to interact with MassMailer
    /// ReSharper disable once InconsistentNaming
    /// </summary>
    class MailWH
    {
        public static string ConnectionString { get; set; }

        // ReSharper disable once InconsistentNaming
        public static long FBLAdd(MessageInfo message)
        {
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlDataAdapter("FBLAdd", conn))
            {
                var outId = new SqlParameter("@ID", SqlDbType.BigInt)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.SelectCommand.CommandType = CommandType.StoredProcedure;
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@DateRecieved", DateTime.Now));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@Subject", message.Mail.Headers.Subject ?? ""));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@MessageID", message.Mail.Headers.MessageId ?? ""));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@AddressTo", message.Mail.Headers.To[0].Address ?? ""));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@AddressFrom", message.Mail.Headers.From.Address ?? ""));
                // TODO: Fill params when FBL will be configured
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@SourceIP", DBNull.Value));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@AuthResult", DBNull.Value));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@FeedBackType", DBNull.Value));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@OriginalRcptTo", DBNull.Value));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@OriginalSubject", DBNull.Value));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@OriginalMailFrom", DBNull.Value));

                cmd.SelectCommand.Parameters.Add(new SqlParameter("@RawMessage", message.Mail.RawMessage));
                cmd.SelectCommand.Parameters.Add(outId);

                conn.Open();
                cmd.SelectCommand.ExecuteNonQuery();

                return outId.Value as long? ?? 0;
            }
        }

        public static long BounceAdd(MessageInfo message)
        {
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlDataAdapter("BounceAdd", conn))
            {
                var body = message.Mail.FindFirstPlainTextVersion() == null
                    ? message.Mail.FindFirstHtmlVersion().GetBodyAsText()
                    : message.Mail.FindFirstPlainTextVersion().GetBodyAsText();

                var outId = new SqlParameter("@ID", SqlDbType.BigInt)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.SelectCommand.CommandType = CommandType.StoredProcedure;
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@RawMessage", message.Mail.RawMessage));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@Message", body));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@Subject", message.Mail.Headers.Subject ?? ""));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@MessageID", message.Mail.Headers.MessageId ?? ""));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@AddressTo", message.Mail.Headers.To[0].Address ?? ""));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@AddressFrom", message.Mail.Headers.From.Address ?? ""));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@DateRecieved", DateTime.Now));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@BounceTypeSysName", (object)message.Subtype ?? DBNull.Value));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@SourceFrom", (object)message.Recipient ?? DBNull.Value));
                // TODO: Add ListId support
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@ListId", DBNull.Value));
                cmd.SelectCommand.Parameters.Add(outId);

                conn.Open();
                cmd.SelectCommand.ExecuteNonQuery();

                return outId.Value as long? ?? 0;
            }
        }
    }
}
