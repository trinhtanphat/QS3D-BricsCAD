using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class CurvedOpeningFootprintRegistration
    {
        [ModuleInitializer]
        internal static void InitializeCurvedOpeningFootprint()
        {
            CurvedOpeningFootprintSmoke.Run();
        }
    }
}
