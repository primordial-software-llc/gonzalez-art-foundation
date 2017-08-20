using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;

namespace SlideshowCreator
{
    class DynamoDbTableFactory
    {
        /// <summary>
        /// Group by artist.
        /// Sort by date.
        /// Use strings for artist and date fields.
        /// </summary>
        /// <returns></returns>
        public CreateTableRequest GetTableDefinition()
        {
            var request = new CreateTableRequest
            {
                TableName = "ImageClassification",
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement
                    {
                        AttributeName = "artist",
                        KeyType = "HASH"
                    },
                    new KeySchemaElement
                    {
                        AttributeName = "name",
                        KeyType = "RANGE"
                    },
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition
                    {
                        AttributeName = "artist",
                        AttributeType = "S"
                    },
                    new AttributeDefinition
                    {
                        AttributeName = "name",
                        AttributeType = "S"
                    }
                },
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = 1,
                    WriteCapacityUnits = 5 // WARNING Lower this once done.
                }
            };

            return request;
        }
    }
}
