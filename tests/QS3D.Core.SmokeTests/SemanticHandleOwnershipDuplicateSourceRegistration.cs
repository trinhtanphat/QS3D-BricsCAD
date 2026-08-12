using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticHandleOwnershipDuplicateSourceRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticHandleOwnershipDuplicateSourceSmoke.Run();
    }
}
