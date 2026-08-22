using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkEditNumericPrecisionRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            BulkEditNumericPrecisionSmoke.Run();
        }
    }
}
