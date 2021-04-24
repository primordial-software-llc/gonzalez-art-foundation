using System.Collections.Generic;
using System.Linq;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using IndexBackend.Model;

namespace IndexBackend.Indexing
{
    public class ImageAnalysis
    {
        public List<ClassificationLabel> GetImageAnalysis(IAmazonRekognition rekognitionClient, string bucket, string s3Path)
        {
            var request = new DetectModerationLabelsRequest
            {
                Image = new Image
                {
                    S3Object = new S3Object
                    {
                        Bucket = bucket,
                        Name = s3Path
                    }
                }
            };
            var response = rekognitionClient.DetectModerationLabelsAsync(request).Result;
            return response.ModerationLabels.Select(x =>
                new ClassificationLabel
                {
                    Confidence = x.Confidence,
                    Name = x.Name,
                    ParentName = x.ParentName
                }).ToList();
        }
    }
}
