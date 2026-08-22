using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class GeometryToleranceFiniteRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => GeometryToleranceFiniteSmoke.Run();
    }
}
