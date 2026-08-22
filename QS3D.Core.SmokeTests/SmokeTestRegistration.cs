using System.Runtime.CompilerServices;
namespace QS3D.Core.SmokeTests
{
    internal static class SmokeTestRegistration
    {
        [ModuleInitializer]
<<<<<<< origin/main
        internal static void Initialize()
        {
            ProjectQuantitySmoke.Run();
            PersistenceHardeningSmoke.Run();
            AdvancedDomainSmoke.Run();
            HardeningRegressionSmoke.Run();
            ReviewHardeningSmoke.Run();
            ContinuationRegressionSmoke.Run();
        }
=======
        internal static void Initialize(){ProjectQuantitySmoke.Run();PersistenceHardeningSmoke.Run();AdvancedDomainSmoke.Run();FullDomainIntegrationSmoke.Run();}
>>>>>>> origin/ci/full-domain-integration-final-20260810
    }
}
