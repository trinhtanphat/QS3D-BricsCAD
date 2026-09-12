using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectPersistenceCheckpointElementDriftSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsPropertyDriftBeforePersistenceRollback();
            RejectsQuantityDriftBeforePersistenceRollback();
            RejectsRelationDriftBeforePersistenceRollback();
            RejectsScalarDriftBeforePersistenceRollback();
            AllowsPersistenceOnlyRollback();
        }

        private static void RejectsPropertyDriftBeforePersistenceRollback()
        {
            VerifySemanticDriftRejected(
                "PROPERTY",
                element => element.SetProperty("WidthM", "1.25"),
                element => Equal("1.25", element.Properties["WidthM"], "Semantic property was changed by rejected restore."));
        }

        private static void RejectsQuantityDriftBeforePersistenceRollback()
        {
            VerifySemanticDriftRejected(
                "QUANTITY",
                element => element.SetQuantity("AreaM2", 12.5),
                element => Equal(12.5, element.Quantities["AreaM2"], "Semantic quantity was changed by rejected restore."));
        }

        private static void RejectsRelationDriftBeforePersistenceRollback()
        {
            VerifySemanticDriftRejected(
                "RELATION",
                element => element.SourceHandles.Add("AB12"),
                element => Equal("AB12", element.SourceHandles[0], "Semantic relation was changed by rejected restore."));
        }

        private static void RejectsScalarDriftBeforePersistenceRollback()
        {
            VerifySemanticDriftRejected(
                "SCALAR",
                element => element.Category = ElementCategory.GlassWall,
                element => Equal(ElementCategory.GlassWall, element.Category, "Semantic scalar was changed by rejected restore."));
        }

        private static void VerifySemanticDriftRejected(
            string suffix,
            Action<ProjectElement> mutate,
            Action<ProjectElement> assertSemanticState)
        {
            var project = new ProjectState("P-CHECKPOINT-ELEMENT-DRIFT-" + suffix, "Checkpoint element drift");
            var element = new ProjectElement("E1", ElementCategory.ArchitecturalWall);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            project.Touch();

            var checkpoint = ProjectPersistenceCheckpoint.Capture(project, new[] { element.Id });
            var projectVersion = project.ChangeVersion;
            var projectUpdatedUtc = project.UpdatedUtc;

            mutate(element);
            var driftDirty = element.Dirty;
            var driftUpdatedUtc = element.UpdatedUtc;

            Equal(false, checkpoint.Matches(project), "Checkpoint still matched after element semantic drift.");
            var exception = Throws<InvalidOperationException>(() => checkpoint.Restore(project));
            Contains(exception.Message, "captured element semantic state changed", "Restore rejection did not identify element semantic drift.");

            assertSemanticState(element);
            Equal(driftDirty, element.Dirty, "Rejected restore rolled element Dirty back to the captured generation.");
            Equal(driftUpdatedUtc, element.UpdatedUtc, "Rejected restore rolled element UpdatedUtc back to the captured generation.");
            Equal(projectVersion, project.ChangeVersion, "Rejected restore changed project ChangeVersion.");
            Equal(projectUpdatedUtc, project.UpdatedUtc, "Rejected restore changed project UpdatedUtc.");
        }

        private static void AllowsPersistenceOnlyRollback()
        {
            var project = new ProjectState("P-CHECKPOINT-ELEMENT-PERSISTENCE", "Checkpoint persistence-only rollback");
            var element = new ProjectElement("E1", ElementCategory.ArchitecturalWall);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            project.Touch();

            var checkpoint = ProjectPersistenceCheckpoint.Capture(project, new[] { element.Id });
            var capturedDirty = element.Dirty;
            var capturedUpdatedUtc = element.UpdatedUtc;
            var projectVersion = project.ChangeVersion;
            var projectUpdatedUtc = project.UpdatedUtc;

            element.MarkDirty(ElementDirtyFlags.Quantity);
            Equal(false, checkpoint.Matches(project), "Checkpoint unexpectedly matched persistence-only drift.");

            checkpoint.Restore(project);

            Equal(capturedDirty, element.Dirty, "Persistence-only restore did not restore captured Dirty.");
            Equal(capturedUpdatedUtc, element.UpdatedUtc, "Persistence-only restore did not restore captured UpdatedUtc.");
            Equal(projectVersion, project.ChangeVersion, "Persistence-only restore changed project ChangeVersion.");
            Equal(projectUpdatedUtc, project.UpdatedUtc, "Persistence-only restore changed project UpdatedUtc.");
        }

        private static TException Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException(
                "ProjectPersistenceCheckpointElementDriftSmoke expected " + typeof(TException).Name + ".");
        }

        private static void Contains(string actual, string expected, string message)
        {
            if (actual.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(
                    "ProjectPersistenceCheckpointElementDriftSmoke " + message + " Actual=" + actual + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    "ProjectPersistenceCheckpointElementDriftSmoke " + message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }
}