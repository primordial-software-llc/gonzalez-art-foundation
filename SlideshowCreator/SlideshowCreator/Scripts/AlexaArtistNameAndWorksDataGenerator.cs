using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using AwsTools;
using GalleryBackend.Model;
using IndexBackend;
using IndexBackend.Indexing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace SlideshowCreator.Scripts
{
    class AlexaArtistNameAndWorksDataGenerator
    {
        private readonly IAmazonDynamoDB client = GalleryAwsCredentialsFactory.DbClient;
        
        /*
         * Lambda function can be 50MB zipped.
         * https://docs.aws.amazon.com/lambda/latest/dg/limits.htmlLambda
         */
        [Test]
        public void Create_Simple_Alexa_Response_Data_Using_Artist_Name_And_Their_Works_Of_Art_With_Data_As_Part_Of_Name()
        {
            const int PAGE_SIZE = 25;
            var path = @"C:\Users\peon\Desktop\projects\SlideshowCreator\SlideshowCreator\alexa-progress.json";
            var scanRequest = new QueryRequest(new ClassificationModel().GetTable());
            QueryResponse scanResponse = null;

            Dictionary<string, HashSet<string>> artistWorks = new Dictionary<string, HashSet<string>>(); // Clean the data during aggregation. Date was added for uniqueness in name. Not sure if duplicates exist where an artist is known for a date.

            if (File.Exists(path))
            {
                var keyText = File.ReadAllText(path);
                var keyParsed = JsonConvert.DeserializeObject<Dictionary<string, AttributeValue>>(keyText);
                scanRequest.ExclusiveStartKey = Conversion<ClassificationModel>.ConvertToPoco(keyParsed).GetKey();
            }
            do
            {
                if (scanResponse != null)
                {
                    scanRequest.ExclusiveStartKey = scanResponse.LastEvaluatedKey;
                }
                scanRequest.KeyConditions = new Dictionary<string, Condition>
                {
                    {
                        "source",
                        new Condition
                        {
                            ComparisonOperator = "EQ",
                            AttributeValueList = new List<AttributeValue>
                            {
                                new AttributeValue {S = new TheAthenaeumIndexer().Source}
                            }
                        }
                    }
                };
                scanRequest.Limit = PAGE_SIZE;
                scanResponse = client.Query(scanRequest);
                var images = Conversion<ClassificationModel>.ConvertToPoco(scanResponse.Items)
                    .ToList();

                foreach (var image in images)
                {
                    if (!artistWorks.ContainsKey(image.Artist))
                    {
                        artistWorks.Add(image.Artist, new HashSet<string>());
                    }
                    artistWorks[image.Artist].Add($"{image.Name} ({image.Date})");
                }
                
                File.WriteAllText(path, JsonConvert.SerializeObject(scanRequest.ExclusiveStartKey));
            } while (scanResponse.LastEvaluatedKey.Any());
            
            JObject americanEnglishData = new JObject();
            foreach (var artistName in artistWorks.Keys.Take(1000)) // There is either invalid data in the json or there is too much data. Using all of the data causes an error when asking "show images by" with any artist name. Alexa error: There was a problem with the requested skill's response
            {
                americanEnglishData.Add(
                    artistName,
                    string.Join(", ", artistWorks[artistName].Take(100))
                );
            }
            /* I would have to generate these translations. AWS has a service for this. */
            JObject britishEnglishData = new JObject();
            britishEnglishData.Add("renoir", "two sisters on the terrace, afternoon boating party, the siene at asnieres");
            britishEnglishData.Add("picasso", "weeping woman, old man and the guitar, sylvette");
            britishEnglishData.Add("edgar degas", "before the races, before the start, finding moses");
            JObject deutschData = new JObject();
            deutschData.Add("renoir", "two sisters on the terrace, afternoon boating party, the siene at asnieres");
            deutschData.Add("picasso", "weeping woman, old man and the guitar, sylvette");
            deutschData.Add("edgar degas", "before the races, before the start, finding moses");
            JObject alexaData = new JObject();
            alexaData.Add("RECIPE_EN_US", americanEnglishData);
            alexaData.Add("RECIPE_EN_GB", britishEnglishData);
            alexaData.Add("RECIPE_DE_DE", deutschData);
            File.WriteAllText(
                @"C:\Users\peon\Desktop\projects\SlideshowCreator\SlideshowCreator\gallery-generated-alexa-data.json",
                $"module.exports = {alexaData.ToString()};");
        }
        
    }
}
