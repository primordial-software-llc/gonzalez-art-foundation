using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;

namespace ArtApi.Model
{
    public interface IModel
    {
        Dictionary<string, AttributeValue> GetKey();
        string GetTable();
    }
}
