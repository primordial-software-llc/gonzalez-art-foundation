using ArtApi.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace IndexBackend.Indexing
{
    public class IndexResult
    {
        public ClassificationModel Model { get; set; }
        /// <summary>
        /// The image must be a jpeg for steps like content moderation and is a best practice for a digital art archive and gallery needs.
        /// See the following method if you encounter source images which aren't .jpg files:
        /// IndexBackend.Indexing.IndexingHttpClient.ConvertToJpeg(byte[] imageBytes)
        /// </summary>
        public Image<Rgba64> ImageJpeg { get; set; }
    }
}
