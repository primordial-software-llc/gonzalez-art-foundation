using System;
using System.IO;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.S3;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ArtApi.Routes.Unauthenticated.CacheEverything
{
    /// <summary>
    /// Thumbnails only now to fix compression limitations.
    /// Create thumbnails in s3 then get rid of this.
    /// This endpoint gets blasted with hundreds of hits for every search.
    /// The infrastructure can handle it no problem, but it's unnecessary.
    /// </summary>
    class GetImage : IRoute
    {
        public string HttpMethod => "GET";
        public string Path => "/unauthenticated/cache-everything/image";
        private const string BUCKET = "images.gonzalez-art-foundation.org";

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
            if (path.Contains("..") || !path.StartsWith("collections/", StringComparison.OrdinalIgnoreCase))
            {
                response.Body = new JObject { { "error", "path is invalid" } }.ToString();
                response.StatusCode = 400;
                return;
            }
            var s3 = new AmazonS3Client();
            var objectImage = s3.GetObjectAsync(BUCKET, $"{path}").Result;
            byte[] bytes;
            using (var stream = objectImage.ResponseStream)
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                bytes = memoryStream.ToArray();
            }
            bytes = CreateThumbnail(bytes);
            response.Headers["Content-Type"] = "image/jpeg";
            response.Body = Convert.ToBase64String(bytes);
            response.IsBase64Encoded = true;
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

        private static Size ResizeKeepAspect(Size src, int maxWidth, int maxHeight)
        {
            maxWidth = Math.Min(maxWidth, src.Width);
            maxHeight = Math.Min(maxHeight, src.Height);

            decimal rnd = Math.Min(maxWidth / (decimal)src.Width, maxHeight / (decimal)src.Height);
            return new Size((int)Math.Round(src.Width * rnd), (int)Math.Round(src.Height * rnd));
        }

    }
}
