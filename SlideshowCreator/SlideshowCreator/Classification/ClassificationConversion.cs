using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
namespace SlideshowCreator.Classification
{
    class ClassificationConversion
    {
        public Dictionary<string, AttributeValue> ConvertToDynamoDb(ClassificationModel classification)
        {
            var kvp = new Dictionary<string, AttributeValue>();
            kvp.Add("pageId", new AttributeValue { N = classification.PageId.ToString() });

            string artist = string.IsNullOrWhiteSpace(classification.Artist)
                ? Classifier.UNKNOWN_ARTIST
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

        /// <summary>
        /// Something is wrong with the de-serialization of the values into the write requests.
        /// </summary>
        /// <remarks>
        /// Poco is plain old class object.
        /// </remarks>
        public ClassificationModel ConvertToPoco(Dictionary<string, AttributeValue> dynamoDbModel)
        {
            var classification = new ClassificationModel();

            classification.PageId = int.Parse(dynamoDbModel["pageId"].N);
            classification.Artist = dynamoDbModel["artist"].S;

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
