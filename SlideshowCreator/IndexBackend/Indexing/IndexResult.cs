using IndexBackend.Model;

namespace IndexBackend.Indexing
{
    public class IndexResult
    {
        public ClassificationModel Model { get; set; }
        public byte[] ImageBytes { get; set; }
    }
}
