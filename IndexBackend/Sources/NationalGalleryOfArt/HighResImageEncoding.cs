using System;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace IndexBackend.Sources.NationalGalleryOfArt
{
    public class HighResImageEncoding
    {
        public static string Decode(string encoded)
        {
            byte[] base64 = Convert.FromBase64String(encoded);
            string decodedBase64 = Encoding.UTF8.GetString(base64);
            var urlDecoded = WebUtility.UrlDecode(decodedBase64);
            return urlDecoded;
        }

        public static string Encode(string decoded)
        {
            var urlEncoded = Uri.EscapeDataString(decoded);
            byte[] base64UrlEncoding = Encoding.UTF8.GetBytes(urlEncoded);
            string base64UrlEncodedText = Convert.ToBase64String(base64UrlEncoding);
            return base64UrlEncodedText;
        }

        public static string CreateReferenceUrlData(string assetId)
        {
            var reference = CreateReference(assetId);
            var jsonReference = JsonConvert.SerializeObject(reference);
            var encoded = Encode(jsonReference);
            return encoded;
        }

        private static HighResImageReference CreateReference(string assetId)
        {
            var reference = new HighResImageReference
            {
                MainForm = new HighResImageReferenceMainForm(),
                Assets = new HighResImageReferenceAssets
                {
                    Asset0 = new HighResImageReferenceAsset {AssetId = assetId}
                }
            };
            return reference;
        }
    }
}
