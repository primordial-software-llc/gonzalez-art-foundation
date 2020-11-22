
using System.Threading.Tasks;
using GalleryBackend.Model;

namespace IndexBackend.Indexing
{
    public interface IIndex
    {
        int GetNextThrottleInMilliseconds { get; }
        Task<ClassificationModel> Index(string id);
    }
}
