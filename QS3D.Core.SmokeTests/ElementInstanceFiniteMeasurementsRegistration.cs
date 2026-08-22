using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class ElementInstanceFiniteMeasurementsRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ElementInstanceFiniteMeasurementsSmoke.Run();
    }
}
