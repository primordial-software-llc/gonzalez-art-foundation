using System.Threading.Tasks;

namespace IndexBackend.Indexing
{
    public interface IIndex
    {
        string ImagePath { get; }
        Task<IndexResult> Index(string id);
    }
}
