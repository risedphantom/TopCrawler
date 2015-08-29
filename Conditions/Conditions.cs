using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using Interfaces;
using OpenPop.Mime;

namespace Conditions
{
    #region --- Types ---
    static class BounceType
    {
        public const string Full = "BounceTypeFull";
        public const string Timeout = "BounceTypeTimeout";
        public const string Refused = "BounceTypeRefused";
        public const string NotFound = "BounceTypeNotFound";
        public const string Inactive = "BounceTypeInactive";
        public const string OutOfOffice = "BounceTypeOutOfOffice";
        public const string HostNotFound = "BounceTypeHostNotFound";
        public const string NotAuthorized = "BounceTypeNotAuthorized";
        public const string ManyConnections = "BounceTypeManyConnections";
    }
    #endregion

    #region --- Not found ---

    [Export(typeof(ICondition))]
    [ExportMetadata("Type", typeof(ConditionNotFound1))]
    public class ConditionNotFound1 : ICondition
    {
        public string Check(Message mimeMessage)
        {
            if (!mimeMessage.MessagePart.IsMultiPart)
                return null;

            const string pattern = "Diagnostic-Code:.+smtp.+550";
            var regexp = new Regex(pattern, RegexOptions.IgnoreCase);

            return mimeMessage.MessagePart.MessageParts.Any(part => part.ContentType.MediaType == "message/delivery-status" && regexp.IsMatch(part.GetBodyAsText())) ? BounceType.NotFound : null;
        }
    }

    [Export(typeof(ICondition))]
    [ExportMetadata("Type", typeof(ConditionNotFound2))]
    public class ConditionNotFound2 : ICondition
    {
        public string Check(Message mimeMessage)
        {
            if (!mimeMessage.MessagePart.IsMultiPart)
                return null;

            const string pattern = "Diagnostic-Code:.+unknown.+user";
            var regexp = new Regex(pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            return mimeMessage.MessagePart.MessageParts.Any(part => part.ContentType.MediaType == "message/delivery-status" && regexp.IsMatch(part.GetBodyAsText())) ? BounceType.NotFound : null;
        }
    }

    [Export(typeof(ICondition))]
    [ExportMetadata("Type", typeof(ConditionNotFound3))]
    public class ConditionNotFound3 : ICondition
    {
        public string Check(Message mimeMessage)
        {
            if (!mimeMessage.MessagePart.IsMultiPart)
                return null;

            const string pattern = "Diagnostic-Code:.+user.+unknown";
            var regexp = new Regex(pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            return mimeMessage.MessagePart.MessageParts.Any(part => part.ContentType.MediaType == "message/delivery-status" && regexp.IsMatch(part.GetBodyAsText())) ? BounceType.NotFound : null;
        }
    }

    [Export(typeof(ICondition))]
    [ExportMetadata("Type", typeof(ConditionNotFound4))]
    public class ConditionNotFound4 : ICondition
    {
        public string Check(Message mimeMessage)
        {
            if (!mimeMessage.MessagePart.IsMultiPart)
                return null;

            const string pattern = "Diagnostic-Code:.+recipient.+address.+rejected";
            var regexp = new Regex(pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            return mimeMessage.MessagePart.MessageParts.Any(part => part.ContentType.MediaType == "message/delivery-status" && regexp.IsMatch(part.GetBodyAsText())) ? BounceType.NotFound : null;
        }
    }

    [Export(typeof(ICondition))]
    [ExportMetadata("Type", typeof(ConditionNotFound5))]
    public class ConditionNotFound5 : ICondition
    {
        public string Check(Message mimeMessage)
        {
            if (!mimeMessage.MessagePart.IsMultiPart)
                return null;

            const string pattern = "Diagnostic-Code:.+recipient.+unknown";
            var regexp = new Regex(pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            return mimeMessage.MessagePart.MessageParts.Any(part => part.ContentType.MediaType == "message/delivery-status" && regexp.IsMatch(part.GetBodyAsText())) ? BounceType.NotFound : null;
        }
    }

    [Export(typeof(ICondition))]
    [ExportMetadata("Type", typeof(ConditionNotFound6))]
    public class ConditionNotFound6 : ICondition
    {
        public string Check(Message mimeMessage)
        {
            if (mimeMessage.MessagePart.IsMultiPart)
                return null;

            const string pattern = "Message was not accepted -- invalid mailbox";
            var regexp = new Regex(pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            return regexp.IsMatch(mimeMessage.MessagePart.GetBodyAsText()) ? BounceType.NotFound : null;
        }
    }

    #endregion

    #region --- Timeout ---

    [Export(typeof(ICondition))]
    [ExportMetadata("Type", typeof(ConditionTimeout1))]
    public class ConditionTimeout1 : ICondition
    {
        public string Check(Message mimeMessage)
        {
            if (!mimeMessage.MessagePart.IsMultiPart)
                return null;

            const string pattern = "Diagnostic-Code:.+Operation.+time.+out";
            var regexp = new Regex(pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            return mimeMessage.MessagePart.MessageParts.Any(part => part.ContentType.MediaType == "message/delivery-status" && regexp.IsMatch(part.GetBodyAsText())) ? BounceType.Timeout : null;
        }
    }

    [Export(typeof(ICondition))]
    [ExportMetadata("Type", typeof(ConditionTimeout2))]
    public class ConditionTimeout2 : ICondition
    {
        public string Check(Message mimeMessage)
        {
            if (!mimeMessage.MessagePart.IsMultiPart)
                return null;

            const string pattern = "Diagnostic-Code:.+conversation.+time.+out";
            var regexp = new Regex(pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            return mimeMessage.MessagePart.MessageParts.Any(part => part.ContentType.MediaType == "message/delivery-status" && regexp.IsMatch(part.GetBodyAsText())) ? BounceType.Timeout : null;
        }
    }
    #endregion

    #region --- Refused ---

    [Export(typeof(ICondition))]
    [ExportMetadata("Type", typeof(ConditionRefused1))]
    public class ConditionRefused1 : ICondition
    {
        public string Check(Message mimeMessage)
        {
            if (!mimeMessage.MessagePart.IsMultiPart)
                return null;

            const string pattern = "Diagnostic-Code:.+Connection.+refused";
            var regexp = new Regex(pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            return mimeMessage.MessagePart.MessageParts.Any(part => part.ContentType.MediaType == "message/delivery-status" && regexp.IsMatch(part.GetBodyAsText())) ? BounceType.Refused : null;
        }
    }

    #endregion

    #region --- Host not found ---

    [Export(typeof(ICondition))]
    [ExportMetadata("Type", typeof(ConditionHostNotFound1))]
    public class ConditionHostNotFound1 : ICondition
    {
        public string Check(Message mimeMessage)
        {
            if (!mimeMessage.MessagePart.IsMultiPart)
                return null;

            const string pattern = "Diagnostic-Code:.+Host.+not.+found";
            var regexp = new Regex(pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            return mimeMessage.MessagePart.MessageParts.Any(part => part.ContentType.MediaType == "message/delivery-status" && regexp.IsMatch(part.GetBodyAsText())) ? BounceType.HostNotFound : null;
        }
    }

    [Export(typeof(ICondition))]
    [ExportMetadata("Type", typeof(ConditionHostNotFound2))]
    public class ConditionHostNotFound2 : ICondition
    {
        public string Check(Message mimeMessage)
        {
            if (!mimeMessage.MessagePart.IsMultiPart)
                return null;

            const string pattern = "Diagnostic-Code:.+No.+route.+to.+host";
            var regexp = new Regex(pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            return mimeMessage.MessagePart.MessageParts.Any(part => part.ContentType.MediaType == "message/delivery-status" && regexp.IsMatch(part.GetBodyAsText())) ? BounceType.HostNotFound : null;
        }
    }

    #endregion

    #region --- Full ---

    [Export(typeof(ICondition))]
    [ExportMetadata("Type", typeof(ConditionFull1))]
    public class ConditionFull1 : ICondition
    {
        public string Check(Message mimeMessage)
        {
            if (!mimeMessage.MessagePart.IsMultiPart)
                return null;

            const string pattern = "Diagnostic-Code:.+over.+quota";
            var regexp = new Regex(pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            return mimeMessage.MessagePart.MessageParts.Any(part => part.ContentType.MediaType == "message/delivery-status" && regexp.IsMatch(part.GetBodyAsText())) ? BounceType.Full : null;
        }
    }

    [Export(typeof(ICondition))]
    [ExportMetadata("Type", typeof(ConditionFull2))]
    public class ConditionFull2 : ICondition
    {
        public string Check(Message mimeMessage)
        {
            if (!mimeMessage.MessagePart.IsMultiPart)
                return null;

            const string pattern = "Diagnostic-Code:.+quota.+exceed";
            var regexp = new Regex(pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            return mimeMessage.MessagePart.MessageParts.Any(part => part.ContentType.MediaType == "message/delivery-status" && regexp.IsMatch(part.GetBodyAsText())) ? BounceType.Full : null;
        }
    }

    #endregion

    #region --- Many connections ---

    [Export(typeof(ICondition))]
    [ExportMetadata("Type", typeof(ConditionManyConnections1))]
    public class ConditionManyConnections1 : ICondition
    {
        public string Check(Message mimeMessage)
        {
            if (!mimeMessage.MessagePart.IsMultiPart)
                return null;

            const string pattern = "Diagnostic-Code:.+too.+many.+connections";
            var regexp = new Regex(pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            return mimeMessage.MessagePart.MessageParts.Any(part => part.ContentType.MediaType == "message/delivery-status" && regexp.IsMatch(part.GetBodyAsText())) ? BounceType.ManyConnections : null;
        }
    }

    #endregion
}
