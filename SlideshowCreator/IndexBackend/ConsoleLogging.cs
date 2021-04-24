using System;

namespace IndexBackend
{
    public class ConsoleLogging : ILogging
    {
        public void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
