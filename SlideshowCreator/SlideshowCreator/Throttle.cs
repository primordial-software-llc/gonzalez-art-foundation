
using System.Threading;
using GalleryBackend.NormalDistributionRandom;

namespace SlideshowCreator
{
    class Throttle
    {
        public void HoldBack()
        {
            var random = new NormalRandomGenerator(1, 1000);
            Thread.Sleep(random.Next());
        }
    }
}
