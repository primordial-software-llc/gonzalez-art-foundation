using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GalleryBackend.Twilio
{
    public class TwilioConfig
    {
        [JsonProperty("twilioSmsPotArn")]
        public string TwilioSmsPotArn { get; set; }

        [JsonProperty("twilioEthereumArn")] 
        public string TwilioEthereumArn { get; set; }

        [JsonProperty("twilioProductionSid")]
        public string TwilioProductionSid { get; set; }

        [JsonProperty("twilioProductionToken")]
        public string TwilioProductionToken { get; set; }

        [JsonProperty("phoneNumberCellPhone")]
        public string PhoneNumberCellPhone { get; set; }

        [JsonProperty("phoneNumberTwilioPurchased")]
        public string PhoneNumberTwilioPurchased { get; set; }

        [JsonProperty("phoneNumberTwilioPurchasedNorthSanDiego")]
        public string PhoneNumberTwilioPurchasedNorthSanDiego { get; set; }

        [JsonProperty("phoneNumberTwilioPurchasedSouthSanDiego")]
        public string PhoneNumberTwilioPurchasedSouthSanDiego { get; set; }

        [JsonProperty("phoneNumberTwilioPurchasedRekognition")]
        public string PhoneNumberTwilioPurchasedRekognition { get; set; }

        [JsonProperty("phoneNumberTwilioPurchasedRekognitionBucket")]
        public string PhoneNumberTwilioPurchasedRekognitionBucket { get; set; }

        [JsonProperty("phoneNumberTwilioPurchasedRekognitionSid")]
        public string PhoneNumberTwilioPurchasedRekognitionSid { get; set; }
    }
}
