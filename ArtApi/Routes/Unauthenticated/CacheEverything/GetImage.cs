using System;
using System.Drawing;
using System.IO;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.S3;
using Newtonsoft.Json.Linq;
using System.Drawing.Imaging;

namespace ArtApi.Routes.Unauthenticated.CacheEverything
{
    class GetImage : IRoute
    {
        public string HttpMethod => "GET";
        public string Path => "/unauthenticated/cache-everything/image";

        public void Run(APIGatewayProxyRequest request, APIGatewayProxyResponse response)
        {
            var path = request.QueryStringParameters != null && request.QueryStringParameters.ContainsKey("path")
                ? request.QueryStringParameters["path"]
                : string.Empty;
            if (string.IsNullOrWhiteSpace(path))
            {
                response.Body = new JObject { { "error", "path is required" } }.ToString();
                response.StatusCode = 400;
                return;
            }
            else if (path.Contains("..") || !path.StartsWith("collections/", StringComparison.OrdinalIgnoreCase))
            {
                response.Body = new JObject { { "error", "path is invalid" } }.ToString();
                response.StatusCode = 400;
                return;
            }
            var s3 = new AmazonS3Client();
            var bucket = "gonzalez-art-foundation";
            var objectImage = s3.GetObjectAsync(bucket, $"{path}").Result;
            var contentType = objectImage.Headers["Content-Type"];

            byte[] bytes;
            using (var stream = objectImage.ResponseStream)
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                bytes = memoryStream.ToArray();

                const double maxMb = 5.5;
                var mb = ConvertBytesToMegabytes(bytes.Length);

                if (mb > maxMb)
                {
                    var image = Image.FromStream(memoryStream);
                    long quality = 95; // Quality isn't linear. A small decrease makes a big impact.
                    EncoderParameter qualityParam = new EncoderParameter(Encoder.Quality, quality);
                    ImageCodecInfo codec = GetEncoderInfo(contentType);
                    EncoderParameters encoderParams = new EncoderParameters(1);
                    encoderParams.Param[0] = qualityParam;

                    using var compressedMemoryStream = new MemoryStream();
                    image.Save(compressedMemoryStream, codec, encoderParams);
                    bytes = compressedMemoryStream.ToArray();
                }
            }

            response.Headers["Content-Type"] = contentType;
            response.Body = Convert.ToBase64String(bytes);
            response.IsBase64Encoded = true;
        }

        static double ConvertBytesToMegabytes(long bytes)
        {
            return (bytes / 1024f) / 1024f;
        }


        /// <summary> 
        /// Returns the image codec with the given mime type 
        /// </summary> 
        private static ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            // Get image codecs for all image formats 
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();

            // Find the correct image codec 
            for (int i = 0; i < codecs.Length; i++)
                if (codecs[i].MimeType == mimeType)
                    return codecs[i];

            return null;
        }
    }
}
