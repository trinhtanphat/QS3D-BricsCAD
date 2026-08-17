using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ElementVerticalPlacementAdditivePrecisionRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ElementVerticalPlacementAdditivePrecisionSmoke.Run();
    }
}
