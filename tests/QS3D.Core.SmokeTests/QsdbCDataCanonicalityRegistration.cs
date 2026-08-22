using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbCDataCanonicalityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QsdbCDataCanonicalitySmoke.Run();
    }
}
