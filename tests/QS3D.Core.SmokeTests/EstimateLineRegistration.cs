using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class EstimateLineRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => EstimateLineSmoke.Run();
    }
}
