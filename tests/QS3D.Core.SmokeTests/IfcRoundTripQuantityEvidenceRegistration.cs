using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class IfcRoundTripQuantityEvidenceRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => IfcRoundTripQuantityEvidenceSmoke.Run();
    }
}
