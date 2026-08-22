using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticScheduleSaveBoundedEnumerationSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticScheduleSaveBoundedEnumerationSmoke.Run();
    }
}
