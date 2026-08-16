using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class LinearRebarClearancePrecisionRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => LinearRebarClearancePrecisionSmoke.Run();
    }
}
