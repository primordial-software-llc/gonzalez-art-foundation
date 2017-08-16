using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace SlideshowCreator
{
    class DataClassificationExecutor
    {

        [Test]
        public void A_Check_Sample()
        {
            var dataDump = new DataDump();
            var niceLandscape = 2594;
            var page = File.ReadAllText(dataDump.GetPageFileNameHtml(niceLandscape));
            var classification = new DataClassifier().Classify(page);
            Assert.AreEqual("The Banks of the River", classification.Name);
            Assert.AreEqual("Charles-FranÃ§ois Daubigny", classification.Artist);
            Assert.AreEqual("Date unknown", classification.Date);
            Assert.AreEqual(5154, classification.ImageId);

            File.WriteAllText(dataDump.GetPageFileNameJson(niceLandscape), JsonConvert.SerializeObject(classification));
            classification = JsonConvert.DeserializeObject<Classification>(File.ReadAllText(dataDump.GetPageFileNameJson(niceLandscape)));
            Assert.AreEqual("The Banks of the River", classification.Name);
            Assert.AreEqual("Charles-FranÃ§ois Daubigny", classification.Artist);
            Assert.AreEqual("Date unknown", classification.Date);
            Assert.AreEqual(5154, classification.ImageId);
        }

        [Test]
        public void B_Check_Sample()
        {
            var dataDump = new DataDump();
            var niceLandscape = 33;
            var page = File.ReadAllText(dataDump.GetPageFileNameHtml(niceLandscape));
            var classification = new DataClassifier().Classify(page);
            Assert.AreEqual("The Mandolin Player", classification.Name);
            Assert.AreEqual("Dante Gabriel Rossetti", classification.Artist);
            Assert.AreEqual("1869", classification.Date);
            Assert.AreEqual(736170, classification.ImageId);
        }

        [Test]
        public void C_Check_Sample_With_Alternbate_Title()
        {
            var dataDump = new DataDump();
            var niceLandscape = 10005;
            var page = File.ReadAllText(dataDump.GetPageFileNameHtml(niceLandscape));
            var classification = new DataClassifier().Classify(page);
            Assert.AreEqual("Rotterdam", classification.Name);
            Assert.AreEqual("Johan Barthold Jongkind", classification.Artist);
            Assert.AreEqual("circa 1871", classification.Date);
            Assert.AreEqual(20117, classification.ImageId);
        }

        [Test]
        public void CA_Check_Sample_With_Alternbate_Title()
        {
            var dataDump = new DataDump();
            var niceLandscape = 10163;
            var page = File.ReadAllText(dataDump.GetPageFileNameHtml(niceLandscape));
            var classification = new DataClassifier().Classify(page);
            Assert.AreEqual("photo of balla in futurist outfit", classification.Name);
            Assert.AreEqual("Artist not listed", classification.Artist);
            Assert.AreEqual(string.Empty, classification.Date);
            Assert.AreEqual(20441, classification.ImageId);
        }
        
        [Test]
        public void CB_Check_Sample_With_Alternbate_Title()
        {
            var dataDump = new DataDump();
            var niceLandscape = 48407;
            var page = File.ReadAllText(dataDump.GetPageFileNameHtml(niceLandscape));
            var classification = new DataClassifier().Classify(page);
            Assert.AreEqual("Man", classification.Name);
            Assert.AreEqual("Artist not listed", classification.Artist);
            Assert.AreEqual(string.Empty, classification.Date);
            Assert.AreEqual(0, classification.ImageId);
        }

        [Test]
        public void D_Classify_All()
        {
            string[] files = Directory.GetFiles(DataDump.HTML_ARCHIVE);

            var dataDump = new DataDump();
            foreach (var fileName in files.Where(x => x.Contains(".html")))
            {
                string rawPageId = fileName
                    .Replace(DataDump.HTML_ARCHIVE + "\\", String.Empty)
                    .Replace(DataDump.FILE_IDENTITY_TEMPLATE, string.Empty)
                    .Replace(".html", string.Empty);

                int pageId = int.Parse(rawPageId);
                File.WriteAllText("C:\\Users\\random\\Desktop\\projects\\SlideshowCreator\\ClassificationProgress.txt", "currentPageId: " + pageId);

                var page = File.ReadAllText(dataDump.GetPageFileNameHtml(pageId));
                var classification = new DataClassifier().Classify(page);
                var json = JsonConvert.SerializeObject(classification);
                File.WriteAllText(dataDump.GetPageFileNameJson(pageId), json);
            }

        }

    }
}
