using System.Collections.Generic;
using System.Linq;
using Amazon;
using Amazon.Lambda;
using IndexBackend;

namespace SlideshowCreator.LambdaSymphony
{
    class BackpageLambdaConfig
    {
        public static List<RegionEndpoint> Regions => RegionEndpoint.EnumerableAllRegions
            .Where(x => x != RegionEndpoint.USGovCloudWest1 && x != RegionEndpoint.CNNorth1).ToList();

        public static string AdIndexerFunctionName = "IndexBackpageAd";

        public static AmazonLambdaClient CreateLambdaClient(RegionEndpoint region)
        {
            AmazonLambdaClient lambdaClient = new AmazonLambdaClient(
                GalleryAwsCredentialsFactory.CreateCredentialsFromDefaultProfile(),
                region);
            return lambdaClient;
        }

    }
}
