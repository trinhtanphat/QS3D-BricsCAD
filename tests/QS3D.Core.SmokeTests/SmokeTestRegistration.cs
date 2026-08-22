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
<<<<<<< origin/main
            AdvancedDomainSmoke.Run();
            HardeningRegressionSmoke.Run();
            ReviewHardeningSmoke.Run();
            ContinuationRegressionSmoke.Run();
=======
            FullDomainSmoke.Run();
            DomainHealthSmoke.Run();
>>>>>>> origin/agent/full-domain-20260810
        }
    }
}
