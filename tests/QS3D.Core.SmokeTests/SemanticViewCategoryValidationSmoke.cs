using System;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticViewCategoryValidationSmoke
    {
        internal static void Run()
        {
            UndefinedCategoryFailsClosed();
            DefinedCategoriesRemainAccepted();
            EmptyCategoryFilterRemainsAccepted();
        }

        private static void UndefinedCategoryFailsClosed()
        {
            MustFailInvalidOperation(
                () => SemanticViewPlanner.Build(
                    BuildProject(),
                    new SemanticViewDefinition(
                        "VIEW-BAD-CATEGORY",
                        "Bad category",
                        categories: new[] { (ElementCategory)999 })),
                "Unsupported semantic view category filter '999'.");
        }

        private static void DefinedCategoriesRemainAccepted()
        {
            foreach (ElementCategory category in Enum.GetValues(typeof(ElementCategory)))
            {
                var plan = SemanticViewPlanner.Build(
                    BuildProject(),
                    new SemanticViewDefinition(
                        "VIEW-" + category,
                        "View " + category,
                        categories: new[] { category }));
                if (plan.ElementIds.Count != 0)
                    throw new Exception("Empty Semantic View category fixture unexpectedly selected elements for " + category + ".");
            }
        }

        private static void EmptyCategoryFilterRemainsAccepted()
        {
            var plan = SemanticViewPlanner.Build(
                BuildProject(),
                new SemanticViewDefinition(
                    "VIEW-ALL",
                    "All categories",
                    categories: Array.Empty<ElementCategory>()));
            if (plan.ElementIds.Count != 0)
                throw new Exception("Empty Semantic View category filter changed empty-project semantics.");
        }

        private static ProjectState BuildProject() =>
            new ProjectState("P-VIEW-CATEGORY", "Semantic View Category Validation");

        private static void MustFailInvalidOperation(Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (!string.Equals(ex.Message, expectedMessage, StringComparison.Ordinal))
                    throw new Exception("Unexpected Semantic View category validation error: " + ex.Message);
                return;
            }
            catch (Exception ex)
            {
                throw new Exception("Expected undefined Semantic View category to fail with InvalidOperationException, got " + ex.GetType().Name + ".", ex);
            }

            throw new Exception("Expected undefined Semantic View category to fail closed.");
        }
    }
}
