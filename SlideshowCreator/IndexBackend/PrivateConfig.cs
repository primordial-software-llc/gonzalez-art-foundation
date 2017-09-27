using System.IO;
using Cryptography;
using Newtonsoft.Json;

namespace IndexBackend
{
    public class PrivateConfig
    {
        [JsonProperty("expectedIp")]
        public string ExpectedIp { get; set; }

        [JsonProperty("ipCheckerUrl")]
        public string IpCheckerUrl { get; set; }

        [JsonProperty("pageNotFoundIndicatorText")]
        public string PageNotFoundIndicatorText { get; set; }

        [JsonProperty("galleryUsername")]
        public string GalleryUsername { get; set; }

        [JsonProperty("galleryPassword")]
        public string GalleryPassword { get; set; }

        [JsonProperty("secretIp")]
        public string SecretIP { get; set; }

        [JsonProperty("secretPadding")]
        public string SecretPadding { get; set; }

        [JsonProperty("secretPassword")]
        public string SecretPassword { get; set; }

        [JsonProperty("secretInitializationVector")]
        public string SecretInitializationVector { get; set; }

        [JsonProperty("nestEncryptedProductId")]
        public string NestEncryptedProductId { get; set; }

        [JsonProperty("nestEncryptedProductSecret")]
        public string NestEncryptedProductSecret { get; set; }

        [JsonProperty("nestEncryptedAuthUrl")]
        public string NestEncryptedAuthUrl { get; set; }

        [JsonProperty("nestEncryptedAccessToken")]
        public string NestEncryptedAccessToken { get; set; }

        [JsonProperty("someUrl")]
        public string SomeUrl { get; set; }

        public string NestDecryptedProductId => Decrypt(NestEncryptedProductId);
        public string NestDecryptedProductSecret => Decrypt(NestEncryptedProductSecret);
        public string NestDecryptedAuthUrl => Decrypt(NestEncryptedAuthUrl);
        public string NestDecryptedAccessToken => Decrypt(NestEncryptedAccessToken);
        public string DecryptedIp => Decrypt(SecretIP);

        public string Decrypt(string encryptedSecret)
        {
            var simpleSymetricCrypto = new SymmetricKeyCryptography();
            var decryptedIp = simpleSymetricCrypto.Decrypt(
                encryptedSecret,
                SecretPassword,
                SecretInitializationVector);
            return decryptedIp.Replace(SecretPadding, string.Empty);
        }

        public string CreateEncryptedValueWithPadding(string unencryptedSecret)
        {
            var simpleSymetricCrypto = new SymmetricKeyCryptography();
            var decryptedIp = simpleSymetricCrypto.Encrypt(
                unencryptedSecret + SecretPadding,
                SecretPassword,
                SecretInitializationVector);
            return decryptedIp.Replace(SecretPadding, string.Empty);
        }

        public static string PersonalJson => "C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\personal.json";

        public static PrivateConfig CreateFromPersonalJson()
        {
            return Create(PersonalJson);
        }

        private static PrivateConfig Create(string fullPath)
        {
            var json = File.ReadAllText(fullPath);
            return JsonConvert.DeserializeObject<PrivateConfig>(json);
        }
    }
}
