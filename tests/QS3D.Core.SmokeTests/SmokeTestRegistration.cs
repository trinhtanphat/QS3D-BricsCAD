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
            RevisionRegressionSmoke.Run();
<<<<<<< HEAD
            WorkflowPersistenceSmoke.Run();
=======
            CompletionRegressionSmoke.Run();
>>>>>>> origin/agent/full-domain-completion-20260810
        }
    }
}
