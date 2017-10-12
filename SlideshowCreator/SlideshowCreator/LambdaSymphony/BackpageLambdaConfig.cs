using System.Collections.Generic;
using System.Linq;
using Amazon;

namespace SlideshowCreator.LambdaSymphony
{
    class BackpageLambdaConfig
    {
        public static List<RegionEndpoint> Regions => RegionEndpoint.EnumerableAllRegions
            .Where(x => x != RegionEndpoint.USGovCloudWest1 && x != RegionEndpoint.CNNorth1).ToList();

        public static string AdIndexerFunctionName = "IndexBackpageAd";

    }
}
