using System;
using System.Threading.Tasks;
using ArtApi.Model;

namespace IndexBackend.Indexing
{
    public interface IIndex : IDisposable
    {
        string ImagePath { get; }
        Task<IndexResult> Index(string id, ClassificationModel existing);
    }
}
