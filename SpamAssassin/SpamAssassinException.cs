using System;

namespace SpamAssassin
{
    public class SpamAssassinException : Exception
    {
        public SpamAssassinException()
        {
        }

        public SpamAssassinException(string message): base(message)
        {
        }

        public SpamAssassinException(string message, Exception innerException): base(message, innerException)
        {
        }
    }
}
