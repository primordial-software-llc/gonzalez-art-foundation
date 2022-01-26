using System;

namespace IndexBackend.Indexing
{
    public class NoIndexContentException : Exception
    {
        public NoIndexContentException(string message)
            : base(message)
        {

        }
    }
}
