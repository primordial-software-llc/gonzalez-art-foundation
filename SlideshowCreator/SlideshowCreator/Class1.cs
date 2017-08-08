using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using NUnit.Framework;

namespace SlideshowCreator
{
    public class Class1
    {
        private readonly Throttle throttle = new Throttle();

        [Test]
        public void Test_VPN()
        {
            // Fill this in on your own with a site which allows programmatic access.

            var wc = new WebClient();
            wc.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36");
            var html = wc.DownloadString(ipCheckerUrl);
            Console.WriteLine(html);

            var expected = $@"{ipCheckerUrl}/ip/{expectedIP}";

            StringAssert.Contains(expected, html);
        }

        // Safety mechanisms.
        private string expectedIP = "";
        private string ipCheckerUrl = "";
        private string targetUrl = "";
        private string pageNotFoundIndicatorText = "";

        [Test]
        public void Check_Sample()
        {
            var dataDump = new DataDump(targetUrl, pageNotFoundIndicatorText);
            int pageId = 33;
            dataDump.Dump(pageId);
            throttle.HoldBack();
            File.WriteAllText("C:\\Users\\random\\Desktop\\projects\\SlideshowCreator\\Progress.txt", "lastPageId: " + pageId);
        }

        [Test]
        public void Check_Page_Contents()
        {
            var dataDump = new DataDump(targetUrl, pageNotFoundIndicatorText);

            for (var pageId = 1; pageId < 288400; pageId += 1)
            {
                dataDump.Dump(pageId);
                throttle.HoldBack();
                File.WriteAllText("C:\\Users\\random\\Desktop\\projects\\SlideshowCreator\\Progress.txt", "lastPageId: " + pageId);
            }
        }

        [Test]
        public void Throttle()
        {
            // With 250,000 works and a throttle of 1 second, what's the estimated time to completion in hours?
            // 250,000 / 60 = 4,166.66 minutes
            // 4,166.66 / 60 = 69.44 hours
            // 69.44 / 2 = 34.72 average hours THROTTLING
            // On a zero latency connection with a throttle of one second it will finish in 35 hours.
            // Now assume double that at a bare minimum for network delays and we are loking at 70 hours to completion.
            // That is being tremendously kind because it's simulating a single user closely with only one connection.
            // That is an artificial limit and combined with a one second max delay in-between that's being rather respectful,
            // because it would probably be worse if I sat here all day and just clicked refresh downloading in parallel in chrome with additional css/js/image references.

            // I'm being incredibly kind and varying the wait time to about a second.
            // Even going so far as to get a statistically even distribution of random numbers to eliminate super short wait times.
            var expectedMaxWaitInMs = 1000;

            var timer = new Stopwatch();
            timer.Start();
            throttle.HoldBack();
            timer.Stop();

            Assert.IsTrue(timer.ElapsedMilliseconds > 1 && timer.ElapsedMilliseconds < expectedMaxWaitInMs);
            Console.WriteLine(timer.ElapsedMilliseconds);

            timer = new Stopwatch();
            timer.Start();
            throttle.HoldBack();
            timer.Stop();
            Assert.IsTrue(timer.ElapsedMilliseconds > 1 && timer.ElapsedMilliseconds < expectedMaxWaitInMs);
            Console.WriteLine(timer.ElapsedMilliseconds);

            timer = new Stopwatch();
            timer.Start();
            throttle.HoldBack();
            timer.Stop();
            Assert.IsTrue(timer.ElapsedMilliseconds > 1 && timer.ElapsedMilliseconds < expectedMaxWaitInMs);
            Console.WriteLine(timer.ElapsedMilliseconds);
        }

    }

}
