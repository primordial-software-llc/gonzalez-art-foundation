using Amazon.Lambda;

namespace IndexBackend.LambdaSymphony
{
    class LambdaFunctionExists
    {
        public bool FunctionExists(AmazonLambdaClient client, string functionName)
        {
            try
            {
                client.GetFunction(functionName);
                return true;
            }
            catch (Amazon.Lambda.Model.ResourceNotFoundException)
            {
                return false;
            }
        }
    }
}
