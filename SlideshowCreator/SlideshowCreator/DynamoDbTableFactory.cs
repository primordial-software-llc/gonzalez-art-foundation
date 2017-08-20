using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;

namespace SlideshowCreator
{
    class DynamoDbTableFactory
    {
        public CreateTableRequest GetTableDefinition()
        {
            var request = new CreateTableRequest
            {
                TableName = "ImageClassification",
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement
                    {
                        AttributeName = "pageId",
                        KeyType = "HASH"
                    },
                    new KeySchemaElement
                    {
                        AttributeName = "artist",
                        KeyType = "RANGE"
                    }
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition
                    {
                        AttributeName = "pageId",
                        AttributeType = "N"
                    },
                    new AttributeDefinition
                    {
                        AttributeName = "artist",
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
