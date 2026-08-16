using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarNotationRoundTripRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RebarNotationRoundTripSmoke.Run();
    }
}
