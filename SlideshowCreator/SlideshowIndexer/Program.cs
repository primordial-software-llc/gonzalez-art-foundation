using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Amazon.DynamoDBv2;
using IndexBackend;
using IndexBackend.Indexing;

namespace SlideshowIndexer
{
    class Program
    {
        /// <summary>
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/aa373208.aspx
        /// https://stackoverflow.com/questions/17921104/preventing-sleep-mode-while-program-runs-c-sharp
        /// </summary>
        internal static class NativeMethods
        {
            [DllImport("kernel32.dll")]
            private static extern uint SetThreadExecutionState(uint esFlags);
            private const uint ES_CONTINUOUS = 0x80000000;
            private const uint ES_SYSTEM_REQUIRED = 0x00000001;
            private const uint ES_AWAYMODE_REQUIRED = 0x00000040;

            public static void PreventSleep()
            {
                SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_AWAYMODE_REQUIRED);
            }

            public static void AllowSleep()
            {
                SetThreadExecutionState(ES_CONTINUOUS);
            }
        }

        private static readonly PrivateConfig PrivateConfig = PrivateConfig.Create("C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\personal.json");
        private static List<string> pageIdQueue;

        static void Main(string[] args)
        {
            var vpnInUse = new VpnCheck().IsVpnInUse(PrivateConfig);

            if (!string.IsNullOrWhiteSpace(vpnInUse))
            {
                Console.WriteLine(vpnInUse);
                return;
            }
            else
            {
                BeginCrawling();
            }
        }

        private static void BeginCrawling()
        {
            pageIdQueue = File
                .ReadAllLines("C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\PageIdQueue.txt")
                .ToList();

            NativeMethods.PreventSleep();

            Console.CancelKeyPress += (sender, eventArgs) => CleanForShutdown();
            
            AmazonDynamoDBClient client = new DynamoDbClientFactory().Create();
            var transientClassifier = new TheAthenaeumIndexer(PrivateConfig, client, ImageClassificationAccess.IMAGE_CLASSIFICATION_V2);
            var throttle = new Throttle();

            try
            {
                for (int i = pageIdQueue.Count - 1; i >= 0; i--)
                {
                    int pageId = int.Parse(pageIdQueue[i]);
                    Console.WriteLine("Classifying page " + pageId);
                    transientClassifier.Index(pageId);
                    pageIdQueue.RemoveAt(i);
                    throttle.HoldBack();
                    File.WriteAllLines("C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\PageIdQueue.txt",
                        pageIdQueue);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                CleanForShutdown();
            }
        }

        private static void CleanForShutdown()
        {
            Console.WriteLine("Shutting down indexer.");
            NativeMethods.AllowSleep();
        }

    }
}
