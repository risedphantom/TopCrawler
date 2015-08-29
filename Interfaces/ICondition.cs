using System;
using OpenPop.Mime;

namespace Interfaces
{
    public interface ICondition
    {
        string Check(Message mimeMessage);
    }

    public interface IConditionMetadata
    {
        Type Type { get; }
    }
}
