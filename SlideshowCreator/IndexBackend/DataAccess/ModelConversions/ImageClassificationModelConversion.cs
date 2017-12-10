
using GalleryBackend;
using GalleryBackend.Model;

namespace IndexBackend.DataAccess.ModelConversions
{
    public class ClassificationConversion : IModelConversion<ClassificationModel>
    {
        public string DynamoDbTableName => ImageClassification.TABLE_IMAGE_CLASSIFICATION;

    }
}
