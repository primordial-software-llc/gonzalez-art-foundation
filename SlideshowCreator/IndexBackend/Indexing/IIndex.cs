
using System.Threading.Tasks;
using GalleryBackend.Model;

namespace IndexBackend.Indexing
{
    public interface IIndex
    {
        string S3Bucket { get; }
        string Source { get; }
        int GetNextThrottleInMilliseconds { get; }
        Task<ClassificationModelNew> Index(int id);
    }
}
