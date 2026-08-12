using System;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticViewKindValidationSmoke
    {
        internal static void Run()
        {
            UndefinedKindFailsClosed();
            DefinedKindsRemainAccepted();
        }

        private static void UndefinedKindFailsClosed()
        {
            var project = BuildProject();
            MustFailInvalidOperation(
                () => SemanticViewPlanner.Build(
                    project,
                    new SemanticViewDefinition("VIEW-BAD-KIND", "Bad kind", (SemanticViewKind)999)),
                "Unsupported semantic view kind '999'.");
        }

        private static void DefinedKindsRemainAccepted()
        {
            AssertAccepted(SemanticViewKind.Model);
            AssertAccepted(SemanticViewKind.Plan);
            AssertAccepted(SemanticViewKind.Schedule);
        }

        private static void AssertAccepted(SemanticViewKind kind)
        {
            var plan = SemanticViewPlanner.Build(
                BuildProject(),
                new SemanticViewDefinition("VIEW-" + kind, "View " + kind, kind));
            if (plan.Kind != kind)
                throw new Exception("Semantic View kind changed during planning. Expected " + kind + ", got " + plan.Kind + ".");
        }

        private static ProjectState BuildProject()
        {
            return new ProjectState("P-VIEW-KIND", "Semantic View Kind Validation");
        }

        private static void MustFailInvalidOperation(Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (!string.Equals(ex.Message, expectedMessage, StringComparison.Ordinal))
                    throw new Exception("Unexpected Semantic View kind validation error: " + ex.Message);
                return;
            }
            catch (Exception ex)
            {
                throw new Exception("Expected undefined Semantic View kind to fail with InvalidOperationException, got " + ex.GetType().Name + ".", ex);
            }

            throw new Exception("Expected undefined Semantic View kind to fail closed.");
        }
    }
}
