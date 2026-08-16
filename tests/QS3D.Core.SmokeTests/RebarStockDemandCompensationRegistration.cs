using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarStockDemandCompensationRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RebarStockDemandCompensationSmoke.Run();
    }
}
