using SixLabors.ImageSharp;
using System;
using System.IO;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace IndexBackend.Indexing
{
    public class ImageCompression
    {
        public static byte[] CreateThumbnail(byte[] bytes, int maxWidth, int maxHeight, IResampler resampler)
        {
            using var image = Image.Load(bytes);
            var thumbnailSize = ResizeKeepAspect(image.Size(), maxWidth, maxHeight);
            image.Mutate(x => x.Resize(thumbnailSize, resampler, false));
            using var compressedMemoryStream = new MemoryStream();
            image.SaveAsJpeg(compressedMemoryStream);
            return compressedMemoryStream.ToArray();
        }

        public static Size ResizeKeepAspect(Size src, int maxWidth, int maxHeight)
        {
            maxWidth = Math.Min(maxWidth, src.Width);
            maxHeight = Math.Min(maxHeight, src.Height);

            decimal rnd = Math.Min(maxWidth / (decimal)src.Width, maxHeight / (decimal)src.Height);
            return new Size((int)Math.Round(src.Width * rnd), (int)Math.Round(src.Height * rnd));
        }
    }
}
