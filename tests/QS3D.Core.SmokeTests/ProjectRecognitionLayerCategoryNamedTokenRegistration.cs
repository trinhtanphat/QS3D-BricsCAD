using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectRecognitionLayerCategoryNamedTokenRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectRecognitionLayerCategoryNamedTokenSmoke.Run();
    }
}
