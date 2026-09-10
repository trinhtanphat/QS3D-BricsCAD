using System;
using QS3D.Core.Features;

namespace QS3D.Core.SmokeTests
{
    internal static class ParityWorkflowRegistrySmoke
    {
        // Intentionally not named Run while #5981 owns SmokeTestRegistration.cs.
        internal static void VerifyUnregisteredContract()
        {
            Throws<ArgumentException>(() => new ParityWorkflowBinding(
                new FeatureId("bim.draw.rectangle"), "bim.draw.rectangle",
                ParityWorkflowKind.SemanticMutation,
                ParityWorkflowSurface.Ui | ParityWorkflowSurface.Mcp,
                ParityWorkflowRequirement.ActiveDocument | ParityWorkflowRequirement.Project));

            var id = new FeatureId("bim.draw.rectangle");
            var safe = new ParityWorkflowBinding(id, "bim.draw.rectangle",
                ParityWorkflowKind.SemanticMutation,
                ParityWorkflowSurface.Ui | ParityWorkflowSurface.Mcp,
                ParityWorkflowRequirement.ActiveDocument | ParityWorkflowRequirement.Project |
                ParityWorkflowRequirement.AtomicMutation | ParityWorkflowRequirement.Audit);
            var registry = new ParityWorkflowRegistry(new[] { safe });
            if (!ReferenceEquals(safe, registry.GetRequired(id)))
                throw new InvalidOperationException("Registry returned the wrong binding.");

            Throws<InvalidOperationException>(() => new ParityWorkflowRegistry(new[] { safe, safe }));
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); } catch (T) { return; }
            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }
    }
}