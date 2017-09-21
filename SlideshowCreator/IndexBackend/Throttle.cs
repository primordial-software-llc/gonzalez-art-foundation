using System.Threading;
using IndexBackend.NormalDistributionRandom;

namespace IndexBackend
{

    // Is this getting used anymore with IIndex?
    public class Throttle
    {
        public void HoldBack()
        {
            var random = new NormalRandomGenerator(1, 1000);
            Thread.Sleep(random.Next());
        }
    }
}
