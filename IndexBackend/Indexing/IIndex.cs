using System.Threading.Tasks;
using ArtApi.Model;

namespace IndexBackend.Indexing
{
    public interface IIndex
    {
        string ImagePath { get; }
        Task<IndexResult> Index(string id, ClassificationModel existing);
    }
}
