using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;
using Amazon;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.S3.Model;
using GalleryBackend;
using GalleryBackend.Twilio;
using IndexBackend;
using Newtonsoft.Json;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Security;
using TwilioSmsHop;

namespace MVC5App.Controllers
{
    [RoutePrefix("api/twilio/rekognition")]
    public class TwilioRekognitionController : ApiController
    {
        
        private TwilioRekognitionDataJson GetVolatileData(TwilioConfig config)
        {
            TwilioRekognitionDataJson data;
            try
            {
                var objResponse = GalleryAwsCredentialsFactory.S3Client.GetObjectAsync(config.PhoneNumberTwilioPurchasedRekognitionBucket, "data.json").Result;
                using (Stream response = objResponse.ResponseStream)
                using (StreamReader reader = new StreamReader(response))
                {
                    data = JsonConvert.DeserializeObject<TwilioRekognitionDataJson>(reader.ReadToEnd());
                }
            }
            catch (Exception)
            {
                data = new TwilioRekognitionDataJson {MessageCount = 1};
                PutObjectRequest request = new PutObjectRequest
                {
                    BucketName = config.PhoneNumberTwilioPurchasedRekognitionBucket,
                    Key = "data.json",
                    ContentBody = JsonConvert.SerializeObject(data)
                };
                var insertResult = GalleryAwsCredentialsFactory.S3Client.PutObjectAsync(request).Result;
            }

            return data;
        }

        private string GetSslUrl()
        {
            return HttpContext.Current.Request.Url.AbsoluteUri.Replace("http://", "https://"); // This is the flow of traffic: Client -> CloudFlare Proxy (https) -> Load Balancer (https) -> Web Server (http)
        }
        
        [Route("sms-pot")]
        public HttpResponseMessage PostMessage()
        {
            var s3Logging = new S3Logging("twilio-rekognition-logs", GalleryAwsCredentialsFactory.S3Client);

            try
            {
                TwilioConfig config;
                using (Stream response = GalleryAwsCredentialsFactory.S3Client.GetObjectAsync("twilio-rekognition", "personal.json").Result.ResponseStream)
                using (StreamReader reader = new StreamReader(response))
                {
                    config = JsonConvert.DeserializeObject<TwilioConfig>(reader.ReadToEnd());
                }

                TwilioClient.Init(config.TwilioProductionSid, config.TwilioProductionToken);

                var requestData = Request.Content.ReadAsStringAsync().Result;
                s3Logging.Log("Incoming request: " + requestData);

                var parameters = requestData.Split('&').ToDictionary(
                    x => WebUtility.UrlDecode(x.Split('=')[0]),
                    x => WebUtility.UrlDecode(x.Split('=')[1])
                );

                try {
                    new RequestValidator(config.TwilioProductionToken)
                        .Validate(
                            GetSslUrl(),
                            parameters,
                            HttpContext.Current.Request.Headers["X-Twilio-Signature"]);
                }
                catch (Exception)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Not from twilio.");
                }
                
                var data = GetVolatileData(config);
                if
                (data.MessageCount >= 1000 ||
                        parameters["From"].Equals(config.PhoneNumberCellPhone) &&
                        parameters["Body"].Equals("hop", StringComparison.OrdinalIgnoreCase))
                {
                    var newNumber = new SmsHop().Hop(config.TwilioProductionSid, config.PhoneNumberTwilioPurchasedRekognitionSid, s3Logging);
                    config.PhoneNumberTwilioPurchasedRekognition = newNumber.PhoneNumber.ToString();
                    config.PhoneNumberTwilioPurchasedRekognitionSid = newNumber.Sid;

                    SaveConfig(config);

                    data.MessageCount = 0;
                    SaveData(config, data);

                    MessageResource.Create(
                        to: parameters["From"],
                        from: config.PhoneNumberTwilioPurchasedRekognition,
                        body:
                        $"New phone number for Twilio Rekognition {config.PhoneNumberTwilioPurchasedRekognition} - " +
                        $"{config.PhoneNumberTwilioPurchasedRekognitionSid}");

                    return GetTwilioSuccess();
                }

                data.MessageCount += 1;
                SaveData(config, data);

                var analysis = AnalyzeImage(parameters["MediaUrl0"], DetectType(parameters["Body"]));
                s3Logging.Log(analysis);

                MessageResource.Create(
                    to: parameters["From"],
                    from: config.PhoneNumberTwilioPurchasedRekognition,
                    body: analysis);

            }
            catch (Exception e)
            {
                s3Logging.Log("Unknown exception " + e);
                throw;
            }

            return GetTwilioSuccess();
        }

        private HttpResponseMessage GetTwilioSuccess()
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            { 
                Content = new StringContent(@"<?xml version=""1.0"" encoding=""UTF-8""?><Response></Response>", Encoding.UTF8, "text/xml")
            };

        }

        private void SaveData(TwilioConfig config, TwilioRekognitionDataJson data)
        {
            GalleryAwsCredentialsFactory.S3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = config.PhoneNumberTwilioPurchasedRekognitionBucket,
                Key = "data.json",
                ContentBody = JsonConvert.SerializeObject(data)
            });
        }

        private void SaveConfig(TwilioConfig config)
        {
            GalleryAwsCredentialsFactory.S3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = config.PhoneNumberTwilioPurchasedRekognitionBucket,
                Key = "personal.json",
                ContentBody = JsonConvert.SerializeObject(config)
            });
        }

        public enum AnalysisType
        {
            Celebrity,
            ContentModeration,
            Labels
        }

        private AnalysisType DetectType(string body)
        {
            if (body.Equals("moderation labels", StringComparison.OrdinalIgnoreCase))
            {
                return AnalysisType.ContentModeration;
            }

            if (body.Equals("labels", StringComparison.OrdinalIgnoreCase))
            {
                return AnalysisType.Labels;
            }

            return AnalysisType.Celebrity;
        }

        public string AnalyzeImage(string imageUrl, AnalysisType analysisType)
        {
            var imageData = new HttpClient().GetByteArrayAsync(imageUrl).Result;
            string reply;
            using (MemoryStream memStream = new MemoryStream(imageData))
            {
                var imageAnalysisClient = new AmazonRekognitionClient(RegionEndpoint.USEast1);

                if (analysisType == AnalysisType.Labels)
                {
                    var analysisRequest = new DetectLabelsRequest();
                    analysisRequest.Image = new Image {Bytes = memStream};
                    var result = imageAnalysisClient.DetectLabelsAsync(analysisRequest).Result;
                    reply = result.Labels.Any()
                        ? string.Join(", ", result.Labels
                            .OrderByDescending(x => x.Confidence)
                            .Select(x => $"{x.Confidence} {x.Name}"))
                        : "No labels found";
                }
                else if (analysisType == AnalysisType.ContentModeration)
                {
                    var analysisRequest = new DetectModerationLabelsRequest();
                    analysisRequest.Image = new Image {Bytes = memStream};
                    var result = imageAnalysisClient.DetectModerationLabels(analysisRequest);
                    reply = result.ModerationLabels.Any()
                        ? string.Join(", ", result.ModerationLabels
                            .OrderByDescending(x => x.Confidence)
                            .Select(x => $"{x.Confidence} {x.ParentName} {x.Name}"))
                        : "No content moderation labels found";
                }
                else
                {
                    var analysisRequest = new RecognizeCelebritiesRequest();
                    analysisRequest.Image = new Image {Bytes = memStream};
                    var result = imageAnalysisClient.RecognizeCelebritiesAsync(analysisRequest).Result;
                    var matches = result.CelebrityFaces
                        .OrderByDescending(x => x.MatchConfidence)
                        .ToList();
                    reply = matches.Any()
                        ? string.Join(", ", matches.Select(x => $"{x.MatchConfidence} {x.Name} {string.Join(", ", x.Urls)}"))
                        : "No celebrities found";
                }
            }
            
            return reply;
        }

    }
}
