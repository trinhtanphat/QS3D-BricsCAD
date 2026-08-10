using System.Runtime.CompilerServices;

namespace QS3D.Core.SmokeTests
{
    internal static class SmokeTestRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ProjectQuantitySmoke.Run();
            PersistenceHardeningSmoke.Run();
            AdvancedDomainSmoke.Run();
            HardeningRegressionSmoke.Run();
            ReviewHardeningSmoke.Run();
            ContinuationRegressionSmoke.Run();
            CompletionRegressionSmoke.Run();
        }
    }
}
