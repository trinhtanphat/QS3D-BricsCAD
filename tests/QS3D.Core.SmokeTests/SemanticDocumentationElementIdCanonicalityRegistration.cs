using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticDocumentationElementIdCanonicalityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticDocumentationElementIdCanonicalitySmoke.Run();
    }
}
