using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class TemplateSaveSizePreflightSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => TemplateSaveSizePreflightSmoke.Run();
    }
}
