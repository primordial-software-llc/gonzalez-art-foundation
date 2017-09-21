
namespace IndexBackend.Indexing
{
    public interface IIndex
    {
        string S3Bucket { get; }
        string Source { get; }
        string IdFileQueuePath { get; }
        int GetNextThrottleInMilliseconds { get; }

        ClassificationModel Index(int id);
    }
}
