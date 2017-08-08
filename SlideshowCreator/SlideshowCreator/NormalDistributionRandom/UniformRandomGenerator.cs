
namespace SlideshowCreator.NormalDistributionRandom
{
    public class UniformRandomGenerator : IRandomGenerator
    {
        private static readonly System.Random threadSafeRandom = new System.Random();
        private MersenneTwister _rGen = new MersenneTwister((uint)threadSafeRandom.Next());

        public int Next()
        {
            return _rGen.Next();
        }

        public double NextDouble()
        {
            return _rGen.NextDouble();
        }

        public int Next(int max)
        {
            return _rGen.Next(max);
        }

        public int Next(int min, int max)
        {
            return _rGen.Next(min, max);
        }
    }
}
