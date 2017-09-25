using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using Amazon.DynamoDBv2;
using GalleryBackend;
using IndexBackend;
using IndexBackend.Indexing;
using NUnit.Framework;
using SlideshowCreator.InfrastructureAsCode;

namespace SlideshowCreator.Tests
{
    class TheAthenaeumIndexingTests
    {
        public const int CONCURRENCY = 100;
        private readonly Throttle throttle = new Throttle();
        private readonly AmazonDynamoDBClient client = new AwsClientFactory().CreateDynamoDbClient();
        private readonly PrivateConfig privateConfig = PrivateConfig.CreateFromPersonalJson();
        private TheAthenaeumIndexer transientClassifier;
        private VpnCheck vpnCheck;

        [OneTimeSetUp]
        public void Setup_All_Tests_Once_And_Only_Once()
        {
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;
            transientClassifier = new TheAthenaeumIndexer(privateConfig.PageNotFoundIndicatorText, client, PublicConfig.TheAthenaeumArt);
            
            vpnCheck = new VpnCheck(new GalleryClient(privateConfig.GalleryUsername, privateConfig.GalleryPassword));
            vpnCheck.AssertVpnInUse(privateConfig.DecryptedIp);
        }

        [Test]
        public void A_Check_Throttle()
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
        public void B_Reclassify_Jean_Leon_Gerome_Sample()
        {
            var classification = transientClassifier.Index(15886);

            Assert.AreEqual("http://www.the-athenaeum.org", classification.Source);
            Assert.AreEqual(15886, classification.PageId);
            Assert.AreEqual(153045, classification.ImageId);
            Assert.AreEqual("The Slave Market", classification.Name); 
            Assert.AreEqual("jean-leon gerome", classification.Artist);
            Assert.AreEqual("Jean-Léon Gérôme", classification.OriginalArtist);
            Assert.AreEqual("1866", classification.Date);
        }

        [Test]
        public void B_Check_Sample1()
        {
            var pageId = 2594;
            var classification = transientClassifier.Index(pageId);
            Assert.AreEqual(pageId, classification.PageId);
            Assert.AreEqual("The Banks of the River", classification.Name);
            Assert.AreEqual("charles-francois daubigny", classification.Artist);
            Assert.AreEqual("Date unknown", classification.Date);
            Assert.AreEqual(5154, classification.ImageId);
        }

        [Test]
        public void B_Check_Sample2()
        {
            var pageId = 33;
            var classification = transientClassifier.Index(pageId);
            Assert.AreEqual("The Mandolin Player", classification.Name);
            Assert.AreEqual("dante gabriel rossetti", classification.Artist);
            Assert.AreEqual("1869", classification.Date);
            Assert.AreEqual(736170, classification.ImageId);
        }

        [Test]
        public void B_Check_Sample_With_Alternbate_Title()
        {
            var pageId = 10005;
            var classification = transientClassifier.Index(pageId);
            Assert.AreEqual("Rotterdam", classification.Name);
            Assert.AreEqual("johan barthold jongkind", classification.Artist);
            Assert.AreEqual("circa 1871", classification.Date);
            Assert.AreEqual(20117, classification.ImageId);
        }

        [Test]
        public void B_Check_Sample_With_Alternbate_Title2()
        {
            var pageId = 10163;
            var classification = transientClassifier.Index(pageId);
            Assert.AreEqual("photo of balla in futurist outfit", classification.Name);
            Assert.AreEqual(Classifier.UNKNOWN_ARTIST, classification.Artist);
            Assert.AreEqual(string.Empty, classification.Date);
            Assert.AreEqual(20441, classification.ImageId);
        }

        [Test]
        public void B_Check_Sample_With_Alternbate_Title3()
        {
            var pageId = 48407;
            var classification = transientClassifier.Index(pageId);
            Assert.AreEqual("Man", classification.Name);
            Assert.AreEqual(Classifier.UNKNOWN_ARTIST, classification.Artist);
            Assert.AreEqual(string.Empty, classification.Date);
            Assert.AreEqual(0, classification.ImageId);
        }

        [Test]
        public void B_Check_Sample_With_Null_Reference_Exception()
        {
            var pageId = 137;
            var classification = transientClassifier.Index(pageId);
            Assert.IsNull(classification);
        }

        [Test]
        public void B_Check_Dynamo_Db_Required_Field_Failure()
        {
            var pageId = 127930;
            var classification = transientClassifier.Index(pageId);
            Assert.AreEqual(Classifier.UNKNOWN_ARTIST, classification.Artist);
            Assert.AreEqual(Classifier.UNKNOWN_ARTIST, classification.OriginalArtist);
        }

        //[Test]
        public void C_Create_Classification_File_Queue()
        {
            List<string> pageIdQueue = new List<string>();
            for (int pageId = 33; pageId < 292400; pageId += 1)
            {
                pageIdQueue.Add(pageId.ToString());
            }
            File.WriteAllLines("C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\PageIdQueue.txt", pageIdQueue);
        }

    }
}
