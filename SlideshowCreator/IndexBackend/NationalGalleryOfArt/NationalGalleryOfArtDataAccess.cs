using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace IndexBackend.NationalGalleryOfArt
{
    public class NationalGalleryOfArtDataAccess
    {
        protected HttpClient Client { get; set; }
        protected Uri Uri { get; }

        public NationalGalleryOfArtDataAccess(Uri uri)
        {
            Uri = uri;
            Client = new HttpClient();
        }

        public string GetSearchResults(int pageNumber)
        {
            string search = $"http://images.nga.gov/en/search/do_advanced_search.html?form_name=default&all_words=&exact_phrase=&exclude_words=&artist_last_name=&keywords_in_title=&accession_number=&school=&Classification=&medium=&year=&year2=&open_access=Open%20Access%20Available&q=&mime_type=&qw=&page={pageNumber}&grid_layout=3";
            return Client.GetStringAsync(search).Result;
        }

        public string GetAssetDetails(string assetId)
        {
            string url = $"https://images.nga.gov/?service=asset&action=show_zoom_window_popup&language=en&asset={assetId}&location=grid&asset_list=46482&basket_item_id=undefined";
            return Client.GetStringAsync(url).Result;
        }

        public async Task<byte[]> GetHighResImageZipFile(string assetId)
        {
            var encodedReference = HighResImageEncoding.CreateReferenceUrlData(assetId);
            var imageDownloadUrl = $"https://images.nga.gov/?service=basket&action=do_direct_download&type=dam&data={encodedReference}";
            Console.WriteLine(imageDownloadUrl);
            var cookieContainer = new CookieContainer();
            var client = new HttpClient(new HttpClientHandler
            {
                CookieContainer = cookieContainer
            });
            var response = await client.GetAsync(imageDownloadUrl);
            byte[] imageZipFile = await response.Content.ReadAsByteArrayAsync();
            
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
