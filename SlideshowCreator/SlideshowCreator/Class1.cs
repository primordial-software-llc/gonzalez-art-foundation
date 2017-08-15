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

        // Safety mechanisms.
        private string expectedIP = "";
        private string ipCheckerUrl = "";
        private string targetUrl = "";
        private string pageNotFoundIndicatorText = "";

        [Test]
        public void A_Test_VPN()
        {
            string html;
            using (var wc = new WebClient())
            {
                wc.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36");
                html = wc.DownloadString(ipCheckerUrl);
            }
            Console.WriteLine(html);
            var expected = $@"{ipCheckerUrl}/ip/{expectedIP}";
            StringAssert.Contains(expected, html);
        }

        /// <summary>
        /// With 250,000 works and a throttle of 1 second, what's the estimated time to completion in hours?
        /// 250,000 / 60 = 4,166.66 minutes
        /// 4,166.66 / 60 = 69.44 hours
        /// 69.44 / 2 = 34.72 average hours THROTTLING
        /// </summary>
        [Test]
        public void B_Check_Throttle()
        {
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

        [Test]
        public void C_Check_Sample()
        {
            var dataDump = new DataDump(targetUrl, pageNotFoundIndicatorText);
            int pageId = 33;
            dataDump.Dump(pageId);
            throttle.HoldBack();
            File.WriteAllText("C:\\Users\\random\\Desktop\\projects\\SlideshowCreator\\Progress.txt", "lastPageId: " + pageId);
        }

        [Test]
        public void D_Dump_All()
        {
            var dataDump = new DataDump(targetUrl, pageNotFoundIndicatorText);

            for (var pageId = 33; pageId < 288400; pageId += 1)
            {
                dataDump.Dump(pageId);
                throttle.HoldBack();
                File.WriteAllText("C:\\Users\\random\\Desktop\\projects\\SlideshowCreator\\Progress.txt", "lastPageId: " + pageId);
            }
        }

    }

}
