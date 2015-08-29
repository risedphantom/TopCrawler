using System;
using OpenPop.Mime;

namespace Interfaces
{
    public interface IIdentification
    {
        string FindRecipient(Message mimeMessage);
    }

    public interface IIdentificationMetadata
    {
        Type Type { get; }
    }
}
