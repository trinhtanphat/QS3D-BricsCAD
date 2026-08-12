using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRuleVariableProjectionSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QuantityRuleVariableProjectionSmoke.Run();
    }
}
