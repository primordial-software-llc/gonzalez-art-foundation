using SixLabors.ImageSharp;
using System;
using System.IO;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace IndexBackend.Indexing
{
    public class ImageCompression
    {
        public static Size DefaultSize = new Size(200, 200);

        public static byte[] CreateThumbnail(byte[] bytes, Size newSize, IResampler resampler)
        {
            using var image = Image.Load(bytes);
            var thumbnailSize = ResizeKeepAspect(image.Size(), newSize);
            image.Mutate(x => x.Resize(thumbnailSize, resampler, false));
            using var compressedMemoryStream = new MemoryStream();
            image.SaveAsJpeg(compressedMemoryStream);
            return compressedMemoryStream.ToArray();
        }

        public static Size ResizeKeepAspect(Size src, Size newSize)
        {
            var maxWidth = Math.Min(newSize.Width, src.Width);
            var maxHeight = Math.Min(newSize.Height, src.Height);

            decimal rnd = Math.Min(maxWidth / (decimal)src.Width, maxHeight / (decimal)src.Height);
            return new Size((int)Math.Round(src.Width * rnd), (int)Math.Round(src.Height * rnd));
        }
    }
}
