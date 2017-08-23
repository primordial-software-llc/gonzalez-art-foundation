﻿using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace SlideshowCreator
{
    public class DataDumpExecutor
    {
        private readonly Throttle throttle = new Throttle();
        private readonly PrivateConfig privateConfig = PrivateConfig.Create("C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\personal.json");

        [Test]
        public void A_Test_VPN()
        {
            new VpnCheck().AssertVpnInUse(privateConfig);
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

            timer = new Stopwatch();
            timer.Start();
            throttle.HoldBack();
            timer.Stop();
            Assert.IsTrue(timer.ElapsedMilliseconds > 1 && timer.ElapsedMilliseconds < expectedMaxWaitInMs);

            timer = new Stopwatch();
            timer.Start();
            throttle.HoldBack();
            timer.Stop();
            Assert.IsTrue(timer.ElapsedMilliseconds > 1 && timer.ElapsedMilliseconds < expectedMaxWaitInMs);
        }

        [Test]
        public void C_Check_Sample()
        {
            var dataDump = new DataDump(privateConfig.TargetUrl, privateConfig.PageNotFoundIndicatorText);
            int pageId = 33;
            dataDump.Dump(pageId);
            throttle.HoldBack();
            File.WriteAllText(PublicConfig.DataDumpProgress, "lastPageId: " + pageId);
        }

        //[Test]
        public void D_Dump_All()
        {
            var dataDump = new DataDump(privateConfig.TargetUrl, privateConfig.PageNotFoundIndicatorText);

            for (var pageId = 33; pageId < 288400; pageId += 1)
            {
                dataDump.Dump(pageId);
                throttle.HoldBack();
                File.WriteAllText("C:\\Users\\random\\Desktop\\projects\\SlideshowCreator\\Progress.txt", "lastPageId: " + pageId);
            }
        }

    }

}
