using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class DocumentationCatalogLoadBoundRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => DocumentationCatalogLoadBoundSmoke.Run();
    }
}
