using System;
using System.Collections.Generic;
using Amazon.SQS;
using ArtApi.Model;
using Microsoft.VisualBasic.FileIO;

namespace IndexBackend.Sources.MetropolitanMuseumOfArt
{
    public class Harvester
    {
        /// <param name="openAccessFilePath">https://github.com/metmuseum/openaccess/blob/master/MetObjects.csv</param>
        public void Harvest(
            IAmazonSQS sqsClient,
            string openAccessFilePath)
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
