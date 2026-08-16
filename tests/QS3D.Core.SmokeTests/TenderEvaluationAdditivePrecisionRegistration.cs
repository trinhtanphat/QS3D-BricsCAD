using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class TenderEvaluationAdditivePrecisionRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => TenderEvaluationAdditivePrecisionSmoke.Run();
    }
}
