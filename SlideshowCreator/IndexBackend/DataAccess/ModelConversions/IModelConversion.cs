using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;

namespace IndexBackend.DataAccess.ModelConversions
{
    public interface IModelConversion<T>
    {
        string DynamoDbTableName { get; }
        Dictionary<string, AttributeValue> ConvertToDynamoDb(T pocoModel);
        T ConvertToPoco(Dictionary<string, AttributeValue> dynamoDbModel);
    }
}
