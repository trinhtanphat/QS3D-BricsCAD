using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ElementInstanceNetConcreteFloorZeroRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ElementInstanceNetConcreteFloorZeroSmoke.Run();
    }
}
