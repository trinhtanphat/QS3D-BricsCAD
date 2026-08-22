using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRevisionReportReadOnlyResultsRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QuantityRevisionReportReadOnlyResultsSmoke.Run();
    }
}
