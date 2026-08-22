using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSelectionBulkNumericPrecisionRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            SemanticSelectionBulkNumericPrecisionSmoke.Run();
        }
    }
}
