using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class IfcRoundTripExchangeResultRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => IfcRoundTripExchangeResultSmoke.Run();
    }
}
