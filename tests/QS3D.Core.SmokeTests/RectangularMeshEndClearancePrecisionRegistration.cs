using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class RectangularMeshEndClearancePrecisionRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RectangularMeshEndClearancePrecisionSmoke.Run();
    }
}
