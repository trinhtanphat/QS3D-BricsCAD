using System;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticMutationOperationCanonicalitySmoke
    {
        internal static void Run()
        {
            PaddedOperationNameIsRejectedBeforeMutation();
            CanonicalOperationNameIsPreservedInJournal();
            ExistingEmptyAndControlGuardsRemainFailClosed();
        }

        private static void PaddedOperationNameIsRejectedBeforeMutation()
        {
            var project = new ProjectState("P-MUTATION-NAME", "Mutation name canonicality");
            var journal = new ProjectSemanticMutationJournal();
            var mutationRan = false;
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;

            Throws<ArgumentException>(() => ProjectSemanticMutationExecutor.Execute(
                project,
                " Bulk edit ",
                () =>
                {
                    mutationRan = true;
                    project.Touch();
                    return 1;
                },
                journal));

            False(mutationRan, "Padded operation name reached the mutation delegate.");
            Equal(beforeVersion, project.ChangeVersion, "Padded operation name changed project version.");
            Equal(beforeUpdatedUtc, project.UpdatedUtc, "Padded operation name changed UpdatedUtc.");
            Equal(0, journal.Entries.Count, "Padded operation name emitted journal entries before rejection.");
        }

        private static void CanonicalOperationNameIsPreservedInJournal()
        {
            var project = new ProjectState("P-MUTATION-NAME-CANONICAL", "Mutation name canonicality control");
            var journal = new ProjectSemanticMutationJournal();

            var result = ProjectSemanticMutationExecutor.Execute(project, "Bulk edit", () => 7, journal);

            Equal(7, result, "Canonical mutation result changed.");
            True(journal.Entries.Count >= 3, "Canonical mutation did not retain normal journal phases.");
            foreach (var entry in journal.Entries)
                Equal("Bulk edit", entry.OperationName, "Canonical operation name changed in the journal.");
        }

        private static void ExistingEmptyAndControlGuardsRemainFailClosed()
        {
            var project = new ProjectState("P-MUTATION-NAME-GUARDS", "Mutation name guard control");
            var mutationRuns = 0;

            Throws<ArgumentException>(() => ProjectSemanticMutationExecutor.Execute(project, "   ", () => ++mutationRuns));
            Throws<ArgumentException>(() => ProjectSemanticMutationExecutor.Execute(project, "Bulk\nedit", () => ++mutationRuns));

            Equal(0, mutationRuns, "Invalid operation names reached mutation execution.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void True(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }

        private static void False(bool value, string message)
        {
            if (value) throw new Exception(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
