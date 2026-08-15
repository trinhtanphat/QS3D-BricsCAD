using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticDocumentationCatalogXmlPersistabilityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticDocumentationCatalogXmlPersistabilitySmoke.Run();
    }
}
