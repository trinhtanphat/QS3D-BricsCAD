using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRevisionSummaryCountIntegritySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QuantityRevisionSummaryCountIntegritySmoke.Run();
    }
}
