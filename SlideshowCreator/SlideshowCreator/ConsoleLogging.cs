using System;
using AwsTools;

namespace SlideshowCreator
{
    class ConsoleLogging : ILogging
    {
        public void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
