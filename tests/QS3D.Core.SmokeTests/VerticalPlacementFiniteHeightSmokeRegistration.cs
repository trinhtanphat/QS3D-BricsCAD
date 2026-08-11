using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class VerticalPlacementFiniteHeightSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => VerticalPlacementFiniteHeightSmoke.Run();
    }
}
