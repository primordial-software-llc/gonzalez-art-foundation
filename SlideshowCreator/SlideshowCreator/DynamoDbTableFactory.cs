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
                    WriteCapacityUnits = 1 // This actually needs to be 400 to do batches of 25 in quick succesion without getting unprocessed items
                }
            };

            return request;
        }
    }
}
