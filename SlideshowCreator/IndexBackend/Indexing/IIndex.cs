
namespace IndexBackend.Indexing
{
    interface IIndex
    {
        string Source { get; }
        ClassificationModel Index(int id);
    }
}
