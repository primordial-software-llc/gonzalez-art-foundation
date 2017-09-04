using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace SlideshowCreator.Classification
{
    class ClassificationExecutor
    {

        [Test]
        public void A_Check_Sample()
        {
            var dataDump = new DataDump();
            var pageId = 2594;
            var page = File.ReadAllText(dataDump.GetPageFileNameHtml(pageId));
            var classification = new Classifier().ClassifyForTheAthenaeum(page, pageId);
            Assert.AreEqual("The Banks of the River", classification.Name);
            Assert.AreEqual("Charles-FranÃ§ois Daubigny", classification.Artist);
            Assert.AreEqual("Date unknown", classification.Date);
            Assert.AreEqual(5154, classification.ImageId);

            File.WriteAllText(dataDump.GetPageFileNameJson(pageId), JsonConvert.SerializeObject((object) classification));
            classification = JsonConvert.DeserializeObject<ClassificationModel>(File.ReadAllText(dataDump.GetPageFileNameJson(pageId)));
            Assert.AreEqual("The Banks of the River", classification.Name);
            Assert.AreEqual("Charles-FranÃ§ois Daubigny", classification.Artist);
            Assert.AreEqual("Date unknown", classification.Date);
            Assert.AreEqual(5154, classification.ImageId);
        }

        [Test]
        public void B_Check_Sample()
        {
            var dataDump = new DataDump();
            var pageId = 33;
            var page = File.ReadAllText(dataDump.GetPageFileNameHtml(pageId));
            var classification = new Classifier().ClassifyForTheAthenaeum(page, pageId);
            Assert.AreEqual("The Mandolin Player", classification.Name);
            Assert.AreEqual("Dante Gabriel Rossetti", classification.Artist);
            Assert.AreEqual("1869", classification.Date);
            Assert.AreEqual(736170, classification.ImageId);
        }

        [Test]
        public void C_Check_Sample_With_Alternbate_Title()
        {
            var dataDump = new DataDump();
            var pageId = 10005;
            var page = File.ReadAllText(dataDump.GetPageFileNameHtml(pageId));
            var classification = new Classifier().ClassifyForTheAthenaeum(page, pageId);
            Assert.AreEqual("Rotterdam", classification.Name);
            Assert.AreEqual("Johan Barthold Jongkind", classification.Artist);
            Assert.AreEqual("circa 1871", classification.Date);
            Assert.AreEqual(20117, classification.ImageId);
        }

        [Test]
        public void CA_Check_Sample_With_Alternbate_Title()
        {
            var dataDump = new DataDump();
            var pageId = 10163;
            var page = File.ReadAllText(dataDump.GetPageFileNameHtml(pageId));
            var classification = new Classifier().ClassifyForTheAthenaeum(page, pageId);
            Assert.AreEqual("photo of balla in futurist outfit", classification.Name);
            Assert.AreEqual("Artist not listed", classification.Artist);
            Assert.AreEqual(string.Empty, classification.Date);
            Assert.AreEqual(20441, classification.ImageId);
        }
        
        [Test]
        public void CB_Check_Sample_With_Alternbate_Title()
        {
            var dataDump = new DataDump();
            var pageId = 48407;
            var page = File.ReadAllText(dataDump.GetPageFileNameHtml(pageId));
            var classification = new Classifier().ClassifyForTheAthenaeum(page, pageId);
            Assert.AreEqual("Man", classification.Name);
            Assert.AreEqual("Artist not listed", classification.Artist);
            Assert.AreEqual(string.Empty, classification.Date);
            Assert.AreEqual(0, classification.ImageId);
        }

        //[Test]
        public void D_Classify_All()
        {
            string[] files = Directory.GetFiles(PublicConfig.HtmlArchive);

            var dataDump = new DataDump();
            foreach (var fileName in files.Where(x => x.Contains(".html")))
            {
                string rawPageId = fileName
                    .Replace(PublicConfig.HtmlArchive + "\\", String.Empty)
                    .Replace(Crawler.FILE_IDENTITY_TEMPLATE, string.Empty)
                    .Replace(".html", string.Empty);

                int pageId = int.Parse(rawPageId);
                File.WriteAllText("C:\\Users\\random\\Desktop\\projects\\SlideshowCreator\\ClassificationProgress.txt", "currentPageId: " + pageId);

                var page = File.ReadAllText(dataDump.GetPageFileNameHtml(pageId));
                var classification = new Classifier().ClassifyForTheAthenaeum(page, pageId);
                var json = JsonConvert.SerializeObject((object) classification);
                File.WriteAllText(dataDump.GetPageFileNameJson(pageId), json);
            }

        }

    }
}
