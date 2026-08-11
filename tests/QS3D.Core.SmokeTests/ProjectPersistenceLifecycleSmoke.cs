using System;
using System.IO;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectPersistenceLifecycleSmoke
    {
        public static void Run()
        {
            StampTracksSemanticChanges();
            SnapshotRollbackRestoresChangeVersion();
            TouchOverflowDoesNotPartiallyMutatePersistenceState();
            StampRejectsAnotherProject();
        }

        private static void StampTracksSemanticChanges()
        {
            var project = new ProjectState("persistence-lifecycle", "Persistence lifecycle");
            var stamp = new ProjectPersistenceStamp(project);
            False(stamp.RequiresSave(project), "A newly tracked unchanged project was marked pending.");

            project.Touch();
            Equal(1L, project.ChangeVersion, "Touch did not advance the project change version.");
            True(stamp.RequiresSave(project), "A semantic change was not marked pending.");

            stamp.MarkSaved(project);
            False(stamp.RequiresSave(project), "A successfully saved project remained pending.");
            project.Touch();
            True(stamp.RequiresSave(project), "A post-save semantic change was not marked pending.");
        }

        private static void SnapshotRollbackRestoresChangeVersion()
        {
            var project = new ProjectState("persistence-rollback", "Persistence rollback");
            project.Touch();
            var expectedVersion = project.ChangeVersion;
            var expectedUpdatedUtc = project.UpdatedUtc;
            var snapshot = ProjectStateSnapshot.Capture(project);

            project.Touch();
            project.Touch();
            snapshot.Restore(project);

            Equal(expectedVersion, project.ChangeVersion, "Project rollback did not restore the change version.");
            Equal(expectedUpdatedUtc, project.UpdatedUtc, "Project rollback did not restore UpdatedUtc.");
        }

        private static void TouchOverflowDoesNotPartiallyMutatePersistenceState()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-touch-overflow-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                File.WriteAllText(path,
                    "<qs3d schema=\"3\" projectId=\"touch-overflow\" name=\"Touch overflow\" updatedUtc=\"2026-08-11T00:00:00.0000000Z\" changeVersion=\"9223372036854775807\" drawingPath=\"\" drawingFingerprint=\"\" activeZoneId=\"\" activeFloorId=\"\">" +
                    "<metadata/><zones/><floors/><families/><rules/><elements/><audit/></qs3d>");
                var project = new QsdbProjectStore().Load(path);
                var expectedUpdatedUtc = project.UpdatedUtc;

                Equal(long.MaxValue, project.ChangeVersion, "Overflow fixture did not load the maximum change version.");
                Throws<OverflowException>(() => project.Touch());
                Equal(long.MaxValue, project.ChangeVersion, "Failed Touch changed the maximum change version.");
                Equal(expectedUpdatedUtc, project.UpdatedUtc, "Failed Touch partially changed UpdatedUtc before overflow.");
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
            }
        }

        private static void StampRejectsAnotherProject()
        {
            var stamp = new ProjectPersistenceStamp(new ProjectState("project-a", "A"));
            Throws<InvalidOperationException>(() => stamp.RequiresSave(new ProjectState("project-b", "B")));
        }

        private static void True(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }

        private static void False(bool value, string message) => True(!value, message);

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual)) throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
