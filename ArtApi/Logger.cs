using System;
using AwsTools;

namespace ArtApi
{
    public class Logger : ILogging
    {
        public void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
