using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2.Model;

namespace GalleryBackend.Classification
{
    public class ClassificationConversion
    {
        /// <summary>
        /// This is needed for the ArtistNameIndex, because keys are required.
        /// Amazon.DynamoDBv2.AmazonDynamoDBException : The provided key element does not match the schema
        /// </summary>
        public const string UNKNOWN_ARTIST = "Unknown";

        public Dictionary<string, AttributeValue> ConvertToDynamoDb(ClassificationModel classification)
        {
            var kvp = new Dictionary<string, AttributeValue>
            {
                {"source", new AttributeValue {S = classification.Source}},
                {"pageId", new AttributeValue {N = classification.PageId.ToString()}}
            };

            string artist = string.IsNullOrWhiteSpace(classification.Artist)
                ? UNKNOWN_ARTIST
                : classification.Artist;

            kvp.Add("artist", new AttributeValue {S = artist });
            kvp.Add(ClassificationModel.ORIGINAL_ARTIST, new AttributeValue {S = classification.OriginalArtist });

            if (classification.ImageId > 0)
            {
                kvp.Add("imageId", new AttributeValue { N = classification.ImageId.ToString() });
            }

            if (!string.IsNullOrWhiteSpace(classification.Name))
            {
                kvp.Add("name", new AttributeValue { S = classification.Name });
            }

            if (!string.IsNullOrWhiteSpace(classification.Date))
            {
                kvp.Add("date", new AttributeValue { S = classification.Date });
            }

            return kvp;
        }
        
        public List<ClassificationModel> ConvertToPoco(List<Dictionary<string, AttributeValue>> models)
        {
            return (from m in models select ConvertToPoco(m)).ToList();
        }

        public ClassificationModel ConvertToPoco(Dictionary<string, AttributeValue> dynamoDbModel)
        {
            var classification = new ClassificationModel
            {
                PageId = int.Parse(dynamoDbModel["pageId"].N),
                Artist = dynamoDbModel["artist"].S
            };

            if (dynamoDbModel.ContainsKey("source"))
            {
                classification.Source = dynamoDbModel["source"].S;
            }

            if (dynamoDbModel.ContainsKey(ClassificationModel.ORIGINAL_ARTIST))
            {
                classification.OriginalArtist = dynamoDbModel[ClassificationModel.ORIGINAL_ARTIST].S;
            }

            if (dynamoDbModel.ContainsKey("date"))
            {
                classification.Date = dynamoDbModel["date"].S;
            }
            if (dynamoDbModel.ContainsKey("name"))
            {
                classification.Name = dynamoDbModel["name"].S;
            }
            if (dynamoDbModel.ContainsKey("imageId"))
            {
                classification.ImageId = int.Parse(dynamoDbModel["imageId"].N);
            }

            return classification;
        }
    }
}
