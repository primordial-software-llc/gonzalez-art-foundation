using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CloudFlareImUnderAttackMode;

namespace IndexBackend.NationalGalleryOfArt
{
    public class NationalGalleryOfArtDataAccess
    {
        protected virtual HttpClient Client { get; set; }
        protected Uri Uri { get; }

        public NationalGalleryOfArtDataAccess(Uri uri)
        {
            Uri = uri;
        }

        public void Init()
        {
            Client?.Dispose();

            var factory = new CloudFlareImUnderAttackModeHttpClientFactory();
            Client = factory.Create(Uri);
        }

        public void SetSearchResultsTo75PerPage()
        {
            Task<string> response =
                Client.GetStringAsync(
                    "http://images.nga.gov/?service=user&action=do_store_grid_layout&layout=3&grid_thumb=7");
            var finalResponse = response.Result;
        }

        public string GetSearchResults(int pageNumber)
        {
            string search = $"http://images.nga.gov/en/search/do_advanced_search.html?form_name=default&all_words=&exact_phrase=&exclude_words=&artist_last_name=&keywords_in_title=&accession_number=&school=&Classification=&medium=&year=&year2=&open_access=Open%20Access%20Available&q=&mime_type=&qw=&page={pageNumber}&grid_layout=3";
            return Client.GetStringAsync(search).Result;
        }

        public string GetAssetDetails(int assetId)
        {
            string url = $"https://images.nga.gov/?service=asset&action=show_zoom_window_popup&language=en&asset={assetId}&location=grid&asset_list=46482&basket_item_id=undefined";
            return Client.GetStringAsync(url).Result;
        }

        public byte[] GetHighResImageZipFile(int assetId)
        {
            var encodedReference = HighResImageEncoding.CreateReferenceUrlData(assetId);
            var imageDownloadUrl =
                $"http://images.nga.gov/?service=basket&action=do_direct_download&type=dam&data={encodedReference}";
            Task<byte[]> asyncImageResponse = Client.GetByteArrayAsync(imageDownloadUrl);
            byte[] imageZipFile = asyncImageResponse.Result;
            
            string imageResponseText = Encoding.UTF8.GetString(imageZipFile);

            var zipPrefix = "PK\u0003\u0004";
            if (!imageResponseText.StartsWith(zipPrefix))
            {
                imageZipFile = null;
            }

            return imageZipFile;
        }
        
    }
}
