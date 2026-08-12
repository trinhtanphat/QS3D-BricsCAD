using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticViewCategoryValidationSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticViewCategoryValidationSmoke.Run();
    }
}
