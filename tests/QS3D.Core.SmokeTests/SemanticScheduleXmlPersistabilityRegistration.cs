using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticScheduleXmlPersistabilityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticScheduleXmlPersistabilitySmoke.Run();
    }
}
