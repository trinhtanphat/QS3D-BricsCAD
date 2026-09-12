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
            RejectsElementSemanticDriftBeforePersistenceRollback();
        }

        private static void RejectsElementSemanticDriftBeforePersistenceRollback()
        {
            var project = new ProjectState("P-CHECKPOINT-ELEMENT-DRIFT", "Checkpoint element drift");
            var element = new ProjectElement("E1", ElementCategory.ArchitecturalWall);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            project.Touch();

            var checkpoint = ProjectPersistenceCheckpoint.Capture(project, new[] { element.Id });
            var projectVersion = project.ChangeVersion;
            var projectUpdatedUtc = project.UpdatedUtc;

            element.SetProperty("WidthM", "1.25");
            var driftDirty = element.Dirty;
            var driftUpdatedUtc = element.UpdatedUtc;

            Throws<InvalidOperationException>(() => checkpoint.Restore(project));

            Equal("1.25", element.Properties["WidthM"], "Semantic property was changed by rejected restore.");
            Equal(driftDirty, element.Dirty, "Rejected restore rolled element Dirty back to the captured generation.");
            Equal(driftUpdatedUtc, element.UpdatedUtc, "Rejected restore rolled element UpdatedUtc back to the captured generation.");
            Equal(projectVersion, project.ChangeVersion, "Rejected restore changed project ChangeVersion.");
            Equal(projectUpdatedUtc, project.UpdatedUtc, "Rejected restore changed project UpdatedUtc.");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException(
                "ProjectPersistenceCheckpointElementDriftSmoke expected " + typeof(TException).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    "ProjectPersistenceCheckpointElementDriftSmoke " + message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
