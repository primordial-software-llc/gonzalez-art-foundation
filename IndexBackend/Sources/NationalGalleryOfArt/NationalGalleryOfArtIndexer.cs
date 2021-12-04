using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using ArtApi.Model;
using IndexBackend.Indexing;

namespace IndexBackend.Sources.NationalGalleryOfArt
{
    public class NationalGalleryOfArtIndexer : IIndex
    {

        public static readonly string TABLE_REVIEW = "gonzalez-art-foundation-image-classification-review";
        public static readonly string BUCKET_REVIEW = "gonzalez-art-foundation-review";
        public string ImagePath => "collections/national-gallery-of-art";
        public string S3Bucket => Constants.IMAGES_BUCKET + "/" + ImagePath;
        public static string Source => "http://images.nga.gov";
        public int GetNextThrottleInMilliseconds => 0;
        protected NationalGalleryOfArtDataAccess NgaDataAccess { get; set; }

        public NationalGalleryOfArtIndexer()
        {
            
        }

        public NationalGalleryOfArtIndexer(NationalGalleryOfArtDataAccess ngaDataAccess)
        {
            NgaDataAccess = ngaDataAccess;
        }

        public async Task<IndexResult> Index(string id, ClassificationModel existing)
        {
            var zipFile = await NgaDataAccess.GetHighResImageZipFile(id);
            if (zipFile == null)
            {
                return null;
            }
            byte[] imageBytes;
            using (MemoryStream zipFileStream = new MemoryStream(zipFile))
            using (ZipArchive archive = new ZipArchive(zipFileStream))
            {
                ZipArchiveEntry imgArchive = archive.Entries
                    .Single(x => ImageExtensions.Any(
                        imgExt => x.FullName.EndsWith(imgExt, StringComparison.OrdinalIgnoreCase)));
                using (var memoryStream = new MemoryStream())
                using (var imgStream = imgArchive.Open())
                {
                    imgStream.CopyTo(memoryStream);
                    imageBytes = memoryStream.ToArray();
                }
                if (!imgArchive.FullName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                {
                    imageBytes = new IndexingHttpClient().ConvertToJpeg(imageBytes).Result;
                }
            }
            var classification = new ClassificationModel
            {
                Source = Source,
                PageId = id
            };
            SetMetaData(classification);
            if (imageBytes == null)
            {
                return null;
            }
            return new IndexResult
            {
                Model = classification,
                ImageJpegBytes = imageBytes
            };
        }

        public void SetMetaData(ClassificationModel model)
        {
            Console.WriteLine("Getting metadata for page id " + model.PageId);
            var html = NgaDataAccess.GetAssetDetails(model.PageId);
            var details = AssetDetailsParser.ParseHtmlToNewModel(html);
            model.OriginalArtist = details.OriginalArtist;
            model.Artist = details.Artist;
            model.Name = details.Name;
            model.Date = details.Date;
            model.SourceLink = details.SourceLink;
        }

        private List<string> ImageExtensions => new List<string> {".jpg",".tif"};

    }
}
