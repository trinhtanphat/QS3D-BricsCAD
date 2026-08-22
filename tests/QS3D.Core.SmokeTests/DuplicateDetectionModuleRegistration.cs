using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class DuplicateDetectionModuleRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            DuplicateDetectionSmoke.Run();
        }
    }
}
