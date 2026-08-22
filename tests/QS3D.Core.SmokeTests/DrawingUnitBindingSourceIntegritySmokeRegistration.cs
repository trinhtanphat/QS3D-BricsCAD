using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class DrawingUnitBindingSourceIntegritySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => DrawingUnitBindingSourceIntegritySmoke.Run();
    }
}
