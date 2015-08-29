using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using Interfaces;
using OpenPop.Mime;

namespace Identification
{
    [Export(typeof(IIdentification))]
    [ExportMetadata("Type", typeof(Identification1))]
    public class Identification1 : IIdentification
    {
        public string FindRecipient(Message mimeMessage)
        {
            if (!mimeMessage.MessagePart.IsMultiPart)
                return null;

            const string pattern = @"Original-Recipient: rfc822;(?<email>.+)\r";
            var regexp = new Regex(pattern, RegexOptions.IgnoreCase);

            foreach (var part in mimeMessage.MessagePart.MessageParts)
            {
                if (part.ContentType.MediaType != "message/delivery-status")
                    continue;
                
                var match = regexp.Match(part.GetBodyAsText());
                if (!match.Success)
                    continue;

                return match.Groups["email"].Value;
            }

            return null;
        }
    }
}
