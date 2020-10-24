using System.Collections.Generic;
using GalleryBackend.Model;
using IndexBackend;
using IndexBackend.DataAccess;

namespace MVC5App
{
    public class DynamoDbClientFactory
    {
        private readonly ImageClassificationAccess access;

        public DynamoDbClientFactory()
        {
            access = new ImageClassificationAccess(GalleryAwsCredentialsFactory.DbClient);
        }


        public List<ImageLabel> SearchByLabel(string label, string source)
        {
            return access.FindByLabel(label, source);
        }

        public ImageLabel GetLabel(int pageId)
        {
            return access.GetLabel(pageId);
        }
        
    }
}