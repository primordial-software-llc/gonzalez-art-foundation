using System;
using AwsTools;

namespace GalleryBackend
{
    public class ConsoleLogging : ILogging
    {
        public void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
