using System;
using System.Data;
using System.Data.SqlClient;

namespace TopCrawler
{
    /// <summary>
    /// Singleton: class to interact with CRM
    /// </summary>
    class Crm
    {
        public static string ConnectionString { get; set; }

        public static long FeedBackIns(MessageInfo message)
        {
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlDataAdapter("FeedBackQueueInsReceivedCRM", conn))
            {
                var body = message.Mail.FindFirstPlainTextVersion() == null
                    ? message.Mail.FindFirstHtmlVersion().GetBodyAsText()
                    : message.Mail.FindFirstPlainTextVersion().GetBodyAsText();

                var bodyEnc = message.Mail.FindFirstPlainTextVersion() == null
                    ? message.Mail.FindFirstHtmlVersion().BodyEncoding.EncodingName
                    : message.Mail.FindFirstPlainTextVersion().BodyEncoding.EncodingName;

                var outId = new SqlParameter("@ID", SqlDbType.BigInt)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.SelectCommand.CommandType = CommandType.StoredProcedure;
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@Date", message.Mail.Headers.DateSent));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@AddressTo", message.Mail.Headers.To[0].Address ?? ""));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@AddressFrom", message.Mail.Headers.From.Address ?? ""));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@NameFrom", message.Mail.Headers.From.DisplayName ?? ""));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@Subject", message.Mail.Headers.Subject ?? ""));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@MessageID", message.Mail.Headers.MessageId ?? ""));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@Body", body ?? ""));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@CharSet", bodyEnc ?? ""));
                cmd.SelectCommand.Parameters.Add(outId);

                conn.Open();
                cmd.SelectCommand.ExecuteNonQuery();

                return outId.Value as long? ?? 0;
            }
        }

        public static void FeedBackAttachIns(long id, string name, byte[] body)
        {
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlDataAdapter("FeedBackQueueAttachmentIns", conn))
            {
                cmd.SelectCommand.CommandType = CommandType.StoredProcedure;
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@FeedBackQueueID", id));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@Name", name));
                cmd.SelectCommand.Parameters.Add(new SqlParameter("@Body", SqlDbType.Image)).Value = body;

                conn.Open();
                cmd.SelectCommand.ExecuteScalar();
            }
        }

        public static void SaveEntireMessage(MessageInfo message)
        {
            //Save FeedBack
            var id = FeedBackIns(message);

            if (id <= 0)
                throw new Exception(String.Format("FeedBack insert failed of message '{0}'. [FeedBackQueueInsReceived] returned {1}", message.Mail.Headers.MessageId, id));

            if (message.Mail.FindAllAttachments().Count == 0)
                return;

            //Save attachments
            foreach (var attach in message.Mail.FindAllAttachments())
                FeedBackAttachIns(id, attach.FileName, attach.Body);
        }
    }
}
