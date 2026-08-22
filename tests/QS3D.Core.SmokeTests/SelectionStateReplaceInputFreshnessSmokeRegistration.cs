using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class SelectionStateReplaceInputFreshnessSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SelectionStateReplaceInputFreshnessSmoke.Run();
    }
}
