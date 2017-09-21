using System;
using System.Runtime.InteropServices;

namespace SlideshowIndexer
{
    /// <inheritdoc />
    /// <summary>
    /// https://msdn.microsoft.com/en-us/library/windows/desktop/aa373208.aspx
    /// https://stackoverflow.com/questions/17921104/preventing-sleep-mode-while-program-runs-c-sharp
    /// </summary>
    public class PreventSleep : IDisposable
    {
        [DllImport("kernel32.dll")]
        private static extern uint SetThreadExecutionState(uint esFlags);
        private const uint ES_CONTINUOUS = 0x80000000;
        private const uint ES_SYSTEM_REQUIRED = 0x00000001;
        private const uint ES_AWAYMODE_REQUIRED = 0x00000040;
        
        public void DontAllowSleep()
        {
            SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_AWAYMODE_REQUIRED);
        }

        public static void AllowSleep()
        {
            SetThreadExecutionState(ES_CONTINUOUS);
        }

        public void Dispose()
        {
            Console.WriteLine("Allowing sleep.");
            AllowSleep();
        }
    }

}
