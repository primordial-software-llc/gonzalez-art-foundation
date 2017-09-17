using System;
using System.IO;
using IndexBackend;
using IndexBackend.NationalGalleryOfArt;
using NUnit.Framework;

namespace SlideshowCreator
{
    class ImagesDotNgaDotGov
    {
        private readonly PrivateConfig privateConfig = PrivateConfig.CreateFromPersonalJson();
        
        [Test]
        public void Get_Home_Page_Through_500_Response()
        {
            new VpnCheck().AssertVpnInUse(privateConfig);
            
            var uri = new Uri(privateConfig.Target2Url);
            var ngaDataAccess = new NgaDataAccess();
            ngaDataAccess.Init(uri);

            var path = "C:\\Data\\NationalGalleryOfArtZippedImages";
            var assetId = 46482;
            ngaDataAccess.DownloadHighResImageZipFileIfExists(assetId, path);
            Assert.IsTrue(File.Exists(path + "\\image-bundle-" + assetId + ".zip"));

            System.Threading.Thread.Sleep(40 * 1000);

            var assetId2 = 1;
            ngaDataAccess.DownloadHighResImageZipFileIfExists(assetId2, path);
            Assert.IsFalse(File.Exists(path + "\\image-bundle-" + assetId2 + ".zip"));
        }

        private const string DECODED_JEAN_LEON_GEROME_VIEW_OF_MEDINET_EL_FAYOUM_HIGH_RES_REFERENCE =
                @"{""mainForm"":{""project_title"":""Personal Digital Gallery"",""usage"":""5""},""assets"":{""a0"":{""assetId"":""135749"",""sizeId"":""3""}}}";

        public const string ENCODED_JEAN_LEON_GEROME_VIEW_OF_MEDINET_EL_FAYOUM_HIGH_RES_REFERENCE = "JTdCJTIybWFpbkZvcm0lMjIlM0ElN0IlMjJwcm9qZWN0X3RpdGxlJTIyJTNBJTIyUGVyc29uYWwlMjBEaWdpdGFsJTIwR2FsbGVyeSUyMiUyQyUyMnVzYWdlJTIyJTNBJTIyNSUyMiU3RCUyQyUyMmFzc2V0cyUyMiUzQSU3QiUyMmEwJTIyJTNBJTdCJTIyYXNzZXRJZCUyMiUzQSUyMjEzNTc0OSUyMiUyQyUyMnNpemVJZCUyMiUzQSUyMjMlMjIlN0QlN0QlN0Q=";

        public const string URL_ENCODED_JEAN_LEON_GEROME_VIEW_OF_MEDINET_EL_FAYOUM_HIGH_RES_REFERENCE = "%7B%22mainForm%22%3A%7B%22project_title%22%3A%22Personal%20Digital%20Gallery%22%2C%22usage%22%3A%225%22%7D%2C%22assets%22%3A%7B%22a0%22%3A%7B%22assetId%22%3A%22135749%22%2C%22sizeId%22%3A%223%22%7D%7D%7D";

        [Test]
        public void Decode_High_Res_Image_Reference()
        {
            Assert.AreEqual(DECODED_JEAN_LEON_GEROME_VIEW_OF_MEDINET_EL_FAYOUM_HIGH_RES_REFERENCE, 
                HighResImageEncoding.Decode(ENCODED_JEAN_LEON_GEROME_VIEW_OF_MEDINET_EL_FAYOUM_HIGH_RES_REFERENCE));
        }

        [Test]
        public void Generate_Encoded_High_Res_Image_Reference()
        {
            Assert.AreEqual(ENCODED_JEAN_LEON_GEROME_VIEW_OF_MEDINET_EL_FAYOUM_HIGH_RES_REFERENCE,
                HighResImageEncoding.Encode(DECODED_JEAN_LEON_GEROME_VIEW_OF_MEDINET_EL_FAYOUM_HIGH_RES_REFERENCE));
        }

        [Test]
        public void Generate_Encoded_High_Res_Image_Reference_From_Typed_Model()
        {
            var reference = HighResImageEncoding.CreateReferenceUrlData(135749);
            Assert.AreEqual(ENCODED_JEAN_LEON_GEROME_VIEW_OF_MEDINET_EL_FAYOUM_HIGH_RES_REFERENCE,
                reference);
        }

    }
}
