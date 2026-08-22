using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class RectangularRebarLayoutScalingUnderflowRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RectangularRebarLayoutScalingUnderflowSmoke.Run();
    }
}
