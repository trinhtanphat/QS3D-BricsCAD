using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticViewKindValidationSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticViewKindValidationSmoke.Run();
    }
}
