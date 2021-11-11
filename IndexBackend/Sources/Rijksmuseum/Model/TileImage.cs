using SixLabors.ImageSharp;

namespace IndexBackend.Sources.Rijksmuseum.Model
{
    public class TileImage
    {
        public int X { get; set; }
        public int Y { get; set; }
        public Image Image { get; set; }
    }
}
