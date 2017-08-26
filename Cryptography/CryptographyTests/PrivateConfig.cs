using System.IO;
using Newtonsoft.Json;

namespace CryptographyTests
{
    class PrivateConfig
    {
        [JsonProperty("githubRecoveryCodesFilePath")]
        public string GithubRecoveryCodesFilePath { get; set; }

        public static PrivateConfig Create(string fullPath)
        {
            var json = File.ReadAllText(fullPath);
            return JsonConvert.DeserializeObject<PrivateConfig>(json);
        }
    }
}
