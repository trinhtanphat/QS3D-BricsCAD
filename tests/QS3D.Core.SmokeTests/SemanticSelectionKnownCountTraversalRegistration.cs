using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSelectionKnownCountTraversalRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticSelectionKnownCountTraversalSmoke.Run();
    }
}
