using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Amazon.S3.Model;
using Amazon.SQS;
using AwsTools;
using GalleryBackend.Model;
using IndexBackend.Indexing;
using Microsoft.VisualBasic.FileIO;

namespace IndexBackend.MetropolitanMuseumOfArt
{
    public class Harvester
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="openAccessFilePath">https://github.com/metmuseum/openaccess/blob/master/MetObjects.csv</param>
        /// <returns></returns>
        public void Harvest(
            IAmazonSQS sqsClient,
            string openAccessFilePath/*, ILogging logging*/)
        {
            var models = new List<ClassificationModel>();
            using (TextFieldParser parser = new TextFieldParser(openAccessFilePath))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                while (!parser.EndOfData)
                {
                    string[] fields = parser.ReadFields();
                    var objectNumber = fields[0];
                    if (string.Equals(objectNumber, "Object Number", StringComparison.OrdinalIgnoreCase))
                    {
                        continue; // Header
                    }
                    var classification = fields[45];
                    if (!classification.Equals("paintings", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    var id = fields[4];
                    var model = new ClassificationModel
                    {
                        Source = MetropolitanMuseumOfArtIndexer.Source,
                        PageId = id
                    };
                    models.Add(model);
                }
            }

            var batches = Batcher.Batch(10, models);
            foreach (var batch in batches)
            {
                IndexBackend.Harvester.SendBatch(sqsClient, batch);
            }
        }
    }
}
