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
            StampTracksDirectNestedPersistedChanges();
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

        private static void StampTracksDirectNestedPersistedChanges()
        {
            var project = new ProjectState("persistence-direct-nested", "Persistence direct nested");
            var family = new ProjectFamily("family-1", "Family 1", ElementCategory.ArchitecturalWall);
            family.Properties["Material"] = "Concrete";
            project.Families.Add(family);
            var element = new ProjectElement("element-1", ElementCategory.ArchitecturalWall, family.Id, string.Empty, string.Empty);
            element.Properties["LengthM"] = "5";
            element.SourceHandles.Add("AB12");
            project.Elements.Add(element);

            var stamp = new ProjectPersistenceStamp(project);
            var expectedVersion = project.ChangeVersion;
            var savedUpdatedUtc = project.UpdatedUtc;
            False(stamp.RequiresSave(project), "Baseline nested persisted state was marked pending.");

            family.Name = "Family 1 revised";
            expectedVersion = checked(expectedVersion + 1L);
            Equal(expectedVersion, project.ChangeVersion, "Direct owned family mutation did not advance the project revision exactly once.");
            True(stamp.RequiresSave(project), "Direct persisted family mutation bypassed dirty detection.");
            family.Name = "Family 1";
            expectedVersion = checked(expectedVersion + 1L);
            Equal(expectedVersion, project.ChangeVersion, "Restoring an owned family scalar did not advance the project revision exactly once.");
            True(stamp.RequiresSave(project), "Restoring an owned family scalar must remain pending until the monotonic project revision is saved.");
            stamp.MarkSaved(project);
            False(stamp.RequiresSave(project), "MarkSaved did not accept the restored family scalar baseline.");
            savedUpdatedUtc = project.UpdatedUtc;

            family.Properties["Material"] = "Brick";
            expectedVersion = checked(expectedVersion + 1L);
            Equal(expectedVersion, project.ChangeVersion, "Direct owned family property mutation did not advance the project revision exactly once.");
            True(stamp.RequiresSave(project), "Direct persisted family property mutation bypassed dirty detection.");
            family.Properties["Material"] = "Concrete";
            expectedVersion = checked(expectedVersion + 1L);
            Equal(expectedVersion, project.ChangeVersion, "Restoring an owned family property did not advance the project revision exactly once.");
            True(stamp.RequiresSave(project), "Restoring an owned family property must remain pending until the monotonic project revision is saved.");
            stamp.MarkSaved(project);
            False(stamp.RequiresSave(project), "MarkSaved did not accept the restored family property baseline.");
            savedUpdatedUtc = project.UpdatedUtc;

            element.Properties["LengthM"] = "6";
            Equal(expectedVersion, project.ChangeVersion, "Direct element property mutation unexpectedly changed the project revision.");
            True(stamp.RequiresSave(project), "Direct persisted element mutation bypassed dirty detection.");
            element.Properties["LengthM"] = "5";
            Equal(expectedVersion, project.ChangeVersion, "Restoring a direct element property unexpectedly changed the project revision.");
            False(stamp.RequiresSave(project), "Restoring the persisted element property left a false-positive dirty state.");

            element.SourceHandles.Add("CD34");
            Equal(expectedVersion, project.ChangeVersion, "Direct element handle mutation unexpectedly changed the project revision.");
            True(stamp.RequiresSave(project), "Direct persisted element handle mutation bypassed dirty detection.");
            element.SourceHandles.Remove("CD34");
            Equal(expectedVersion, project.ChangeVersion, "Restoring direct element handles unexpectedly changed the project revision.");
            False(stamp.RequiresSave(project), "Restoring the persisted element handles left a false-positive dirty state.");

            project.Zones.Add(new ZoneDefinition("zone-1", "Zone 1"));
            expectedVersion = checked(expectedVersion + 1L);
            Equal(expectedVersion, project.ChangeVersion, "Direct Zone catalog add did not advance the project revision exactly once.");
            True(stamp.RequiresSave(project), "Direct persisted Zone catalog mutation bypassed dirty detection.");
            project.Zones.Clear();
            expectedVersion = checked(expectedVersion + 1L);
            Equal(expectedVersion, project.ChangeVersion, "Restoring the Zone catalog did not advance the project revision exactly once.");
            True(stamp.RequiresSave(project), "Restoring Zone catalog content must remain pending until the monotonic project revision is saved.");
            stamp.MarkSaved(project);
            False(stamp.RequiresSave(project), "MarkSaved did not accept the restored Zone catalog baseline.");
            savedUpdatedUtc = project.UpdatedUtc;

            project.UpdatedUtc = savedUpdatedUtc.AddSeconds(1);
            Equal(expectedVersion, project.ChangeVersion, "Direct persisted timestamp mutation unexpectedly changed the project revision.");
            True(stamp.RequiresSave(project), "Direct persisted project timestamp mutation bypassed dirty detection.");
            project.UpdatedUtc = savedUpdatedUtc;
            Equal(expectedVersion, project.ChangeVersion, "Restoring the direct persisted timestamp unexpectedly changed the project revision.");
            False(stamp.RequiresSave(project), "Restoring the persisted project timestamp left a false-positive dirty state.");

            family.Name = "Saved family";
            expectedVersion = checked(expectedVersion + 1L);
            Equal(expectedVersion, project.ChangeVersion, "Pre-save owned family mutation did not advance the project revision exactly once.");
            stamp.MarkSaved(project);
            False(stamp.RequiresSave(project), "MarkSaved did not refresh the nested persisted-content baseline.");
            family.Name = "Family 1";
            expectedVersion = checked(expectedVersion + 1L);
            Equal(expectedVersion, project.ChangeVersion, "Post-save owned family mutation did not advance the project revision exactly once.");
            True(stamp.RequiresSave(project), "A post-MarkSaved nested mutation was not detected.");
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
