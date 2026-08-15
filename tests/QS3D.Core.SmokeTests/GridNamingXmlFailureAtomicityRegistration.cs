using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class GridNamingXmlFailureAtomicityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => GridNamingXmlFailureAtomicitySmoke.Run();
    }
}
