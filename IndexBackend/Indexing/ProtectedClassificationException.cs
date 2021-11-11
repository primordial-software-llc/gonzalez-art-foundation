using System;

namespace IndexBackend.Indexing
{
    public class ProtectedClassificationException : Exception
    {
        public ProtectedClassificationException(string message)
            : base(message)
        {

        }
    }
}
