
namespace IndexBackend.Indexing
{
    interface IIndex
    {
        string S3Bucket { get; }
        string Source { get; }
        ClassificationModel Index(int id);
    }
}
