using System;
using System.IO;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.S3;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace ArtApi.Routes.Unauthenticated.CacheEverything
{
    class GetImage : IRoute
    {
        public string HttpMethod => "GET";
        public string Path => "/unauthenticated/cache-everything/image";
        private const double MAX_MEGABYTES = 5.5;
        private const string BUCKET = "gonzalez-art-foundation";

        public void Run(APIGatewayProxyRequest request, APIGatewayProxyResponse response)
        {
            var path = request.QueryStringParameters != null && request.QueryStringParameters.ContainsKey("path")
                ? request.QueryStringParameters["path"]
                : string.Empty;
            var thumbnail = request.QueryStringParameters != null && request.QueryStringParameters.ContainsKey("thumbnail")
                ? request.QueryStringParameters["thumbnail"]
                : string.Empty;
            if (string.IsNullOrWhiteSpace(path))
            {
                response.Body = new JObject { { "error", "path is required" } }.ToString();
                response.StatusCode = 400;
                return;
            }
            if (path.Contains("..") || !path.StartsWith("collections/", StringComparison.OrdinalIgnoreCase))
            {
                response.Body = new JObject { { "error", "path is invalid" } }.ToString();
                response.StatusCode = 400;
                return;
            }
            var s3 = new AmazonS3Client();
            var objectImage = s3.GetObjectAsync(BUCKET, $"{path}").Result;
            var contentType = objectImage.Headers["Content-Type"];
            byte[] bytes;
            using (var stream = objectImage.ResponseStream)
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                bytes = memoryStream.ToArray();
            }
            if (string.Equals(thumbnail, "thumbnail", StringComparison.Ordinal)) // Compare by case so cache can't be broken by changing the case
            {
                bytes = CreateThumbnail(bytes);
                contentType = "image/jpeg";
            }
            else if (ConvertBytesToMegabytes(bytes.Length) > MAX_MEGABYTES)
            {
                bytes = GetCompressed(bytes);
                contentType = "image/jpeg";
            }
            response.Headers["Content-Type"] = contentType;
            response.Body = Convert.ToBase64String(bytes);
            response.IsBase64Encoded = true;
        }

        private byte[] GetCompressed(byte[] bytes)
        {
            using var image = Image.Load(bytes);
            var encoder = new JpegEncoder
            {
                Quality = 90
            };
            using var compressedMemoryStream = new MemoryStream();
            image.Save(compressedMemoryStream, encoder);
            return compressedMemoryStream.ToArray();
        }

        private static byte[] CreateThumbnail(byte[] bytes)
        {
            using var image = Image.Load(bytes);
            var thumbnailSize = ResizeKeepAspect(image.Size(), 200, 200);
            image.Mutate(x => x.Resize(thumbnailSize));
            using var compressedMemoryStream = new MemoryStream();
            image.SaveAsJpeg(compressedMemoryStream);
            return compressedMemoryStream.ToArray();
        }

        private static double ConvertBytesToMegabytes(long bytes)
        {
            return bytes / 1024f / 1024f;
        }

        private static Size ResizeKeepAspect(Size src, int maxWidth, int maxHeight)
        {
            maxWidth = Math.Min(maxWidth, src.Width);
            maxHeight = Math.Min(maxHeight, src.Height);

            decimal rnd = Math.Min(maxWidth / (decimal)src.Width, maxHeight / (decimal)src.Height);
            return new Size((int)Math.Round(src.Width * rnd), (int)Math.Round(src.Height * rnd));
        }

    }
}
