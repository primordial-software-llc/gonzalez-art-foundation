using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2.Model;

namespace IndexBackend
{
    public class ClassificationConversion
    {

        public Dictionary<string, AttributeValue> ConvertToDynamoDb(ClassificationModel classification)
        {
            var kvp = new Dictionary<string, AttributeValue>
            {
                {"source", new AttributeValue {S = classification.Source}},
                {"pageId", new AttributeValue {N = classification.PageId.ToString()}}
            };

            if (!string.IsNullOrWhiteSpace(classification.Artist))
            {
                kvp.Add("artist", new AttributeValue {S = classification.Artist});
            }

            if (!string.IsNullOrWhiteSpace(classification.OriginalArtist))
            {
                kvp.Add(ClassificationModel.ORIGINAL_ARTIST, new AttributeValue {S = classification.OriginalArtist});
            }

            if (classification.ImageId > 0)
            {
                kvp.Add("imageId", new AttributeValue {N = classification.ImageId.ToString()});
            }

            if (!string.IsNullOrWhiteSpace(classification.Name))
            {
                kvp.Add("name", new AttributeValue {S = classification.Name});
            }

            if (!string.IsNullOrWhiteSpace(classification.Date))
            {
                kvp.Add("date", new AttributeValue {S = classification.Date});
            }

            if (!string.IsNullOrWhiteSpace(classification.S3Path))
            {
                kvp.Add("s3Path", new AttributeValue {S = classification.S3Path});
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
                Source = dynamoDbModel["source"].S
            };

            if (dynamoDbModel.ContainsKey("artist")) // Could use key to JsonProperty relialbly here, if the same is done on the opposite conversion. That would clean all this crap up, then make it reusable for other tables. Otherwise this is going to get "heavy" if there were 50 tables. What about datatype? Another property? Does that already exist? A dynamodb mapper? I think so.
            {
                classification.Artist = dynamoDbModel["artist"].S;
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

            if (dynamoDbModel.ContainsKey("s3Path"))
            {
                classification.S3Path = dynamoDbModel["s3Path"].S;
            }

            return classification;
        }
    }
}
