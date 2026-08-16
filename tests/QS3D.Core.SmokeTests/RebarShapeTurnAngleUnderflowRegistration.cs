using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarShapeTurnAngleUnderflowRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RebarShapeTurnAngleUnderflowSmoke.Run();
    }
}
