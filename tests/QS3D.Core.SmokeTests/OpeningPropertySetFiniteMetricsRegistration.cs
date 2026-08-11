using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class OpeningPropertySetFiniteMetricsRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => OpeningPropertySetFiniteMetricsSmoke.Run();
    }
}
