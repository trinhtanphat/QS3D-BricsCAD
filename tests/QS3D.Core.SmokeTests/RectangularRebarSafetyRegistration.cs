using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class RectangularRebarSafetyRegistration
    {
        [ModuleInitializer]
        internal static void InitializeRectangularRebarSafety()
        {
            RectangularRebarSafetySmoke.Run();
        }
    }
}
