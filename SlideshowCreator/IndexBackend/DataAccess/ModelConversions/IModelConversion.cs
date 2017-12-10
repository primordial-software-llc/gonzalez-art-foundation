
namespace IndexBackend.DataAccess.ModelConversions
{
    public interface IModelConversion<T>
    {
        string DynamoDbTableName { get; }
    }
}
