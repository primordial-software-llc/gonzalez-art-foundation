
using System.IO;
using Newtonsoft.Json;

namespace SlideshowCreator
{
    class PrivateConfig
    {

        [JsonProperty("expectedIp")]
        public string ExpectedIp { get; set; }

        [JsonProperty("ipCheckerUrl")]
        public string IpCheckerUrl { get; set; }

        [JsonProperty("targetUrl")]
        public string TargetUrl { get; set; }

        [JsonProperty("pageNotFoundIndicatorText")]
        public string PageNotFoundIndicatorText { get; set; }

        [JsonProperty("target2Url")]
        public string Target2Url { get; set; }

        [JsonProperty("githubRecoveryCodesFilePath")]
        public string GithubRecoveryCodesFilePath { get; set; }

        public static PrivateConfig Create(string fullPath)
        {
            var json = File.ReadAllText(fullPath);
            return JsonConvert.DeserializeObject<PrivateConfig>(json);
        }
    }
}
