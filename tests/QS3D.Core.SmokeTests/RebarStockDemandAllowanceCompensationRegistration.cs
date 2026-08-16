using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarStockDemandAllowanceCompensationRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RebarStockDemandAllowanceCompensationSmoke.Run();
    }
}
