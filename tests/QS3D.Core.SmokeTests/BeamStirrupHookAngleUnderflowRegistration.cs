using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class BeamStirrupHookAngleUnderflowRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => BeamStirrupHookAngleUnderflowSmoke.Run();
    }
}
