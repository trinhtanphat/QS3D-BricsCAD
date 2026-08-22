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
            LogicRegressionSmoke.Run();
<<<<<<< origin/main
            RevisionRegressionSmoke.Run();
            WorkflowPersistenceSmoke.Run();
=======
            CompletionRegressionSmoke.Run();
>>>>>>> 7af2854
        }
    }
}
