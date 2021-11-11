using System;

namespace IndexBackend.Sources.Rijksmuseum
{
    public class StitchedImageException : Exception
    {
        public StitchedImageException(string message)
            : base(message)
        {

        }
    }
}
