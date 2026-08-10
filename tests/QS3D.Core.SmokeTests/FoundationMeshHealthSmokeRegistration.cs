using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class FoundationMeshHealthSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            FoundationMeshHealthSmoke.Run();
        }
    }
}
