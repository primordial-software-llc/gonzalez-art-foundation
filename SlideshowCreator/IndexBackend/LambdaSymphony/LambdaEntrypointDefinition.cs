
namespace IndexBackend.LambdaSymphony
{
    public class LambdaEntrypointDefinition
    {
        public string AssemblyName { get; set; }
        public string Namespace { get; set; }
        public string ClassName { get; set; }
        public string FunctionName { get; set; }

        public string GetEntryPointHandler()
        {
            return $"{AssemblyName}::{Namespace}.{ClassName}::{FunctionName}";
        }
    }
}
