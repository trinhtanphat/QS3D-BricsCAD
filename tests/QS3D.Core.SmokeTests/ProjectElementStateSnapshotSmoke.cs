using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectElementStateSnapshotSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RestoresExactElementWithoutPublishingProjectRevision();
            RejectsReplacementElementGeneration();
        }

        private static void RestoresExactElementWithoutPublishingProjectRevision()
        {
            var project = new ProjectState("P-ELEMENT-SNAPSHOT", "Element snapshot");
            var owner = NewOwner("E1");
            var unrelated = NewOwner("E2");
            unrelated.SetProperty("Sentinel", "keep");
            project.Elements.Add(owner);
            project.Elements.Add(unrelated);
            project.Touch();
            var expectedCategory = owner.Category;
            var expectedFamily = owner.FamilyId;
            var expectedFloor = owner.FloorId;
            var expectedZone = owner.ZoneId;
            var expectedFingerprint = owner.DrawingFingerprint;
            var expectedDirty = owner.Dirty;
            var expectedUpdatedUtc = owner.UpdatedUtc;
            var snapshot = ProjectElementStateSnapshot.Capture(project, owner.Id);

            owner.Category = ElementCategory.StructuralWall;
            owner.FamilyId = "F2";
            owner.FloorId = "L2";
            owner.ZoneId = "Z2";
            owner.DrawingFingerprint = "drawing-b";
            owner.SourceHandles.Clear();
            owner.SourceHandles.Add("BB");
            owner.DependsOn.Clear();
            owner.DependsOn.Add("DEP-B");
            owner.SetProperty("WidthM", "9.99");
            owner.SetProperty("GeneratedSolidHandle", "B2");
            owner.SetQuantity("GrossVolumeM3", 99.0d);

            var projectVersionBeforeRestore = project.ChangeVersion;
            var projectUpdatedBeforeRestore = project.UpdatedUtc;
            snapshot.Restore(project);
            Equal(expectedCategory, owner.Category, "Category drifted.");
            Equal(expectedFamily, owner.FamilyId, "FamilyId drifted.");
            Equal(expectedFloor, owner.FloorId, "FloorId drifted.");
            Equal(expectedZone, owner.ZoneId, "ZoneId drifted.");
            Equal(expectedFingerprint, owner.DrawingFingerprint, "DrawingFingerprint drifted.");
            Equal("AA", owner.SourceHandles[0], "SourceHandles drifted.");
            Equal("DEP-A", owner.DependsOn[0], "DependsOn drifted.");
            Equal("1.25", owner.Properties["WidthM"], "Properties drifted.");
            Equal("A1", owner.Properties["GeneratedSolidHandle"], "Generated owner state drifted.");
            Equal(12.5d, owner.Quantities["GrossVolumeM3"], "Quantities drifted.");
            Equal(expectedDirty, owner.Dirty, "Dirty drifted.");
            Equal(expectedUpdatedUtc, owner.UpdatedUtc, "UpdatedUtc drifted.");
            Equal(projectVersionBeforeRestore, project.ChangeVersion, "Restore published project revision.");
            Equal(projectUpdatedBeforeRestore, project.UpdatedUtc, "Restore published project timestamp.");
            Equal("keep", unrelated.Properties["Sentinel"], "Unrelated element changed.");
            True(snapshot.Matches(project), "Restored state does not match scoped snapshot.");
        }

        private static void RejectsReplacementElementGeneration()
        {
            var project = new ProjectState("P-ELEMENT-SNAPSHOT-GENERATION", "Element generation");
            var owner = NewOwner("E1");
            project.Elements.Add(owner);
            project.Touch();
            var snapshot = ProjectElementStateSnapshot.Capture(project, owner.Id);
            project.Elements.Remove(owner);
            var replacement = NewOwner("E1");
            project.Elements.Add(replacement);
            Throws<InvalidOperationException>(() => snapshot.Restore(project));
            Equal("A1", replacement.Properties["GeneratedSolidHandle"], "Rejected restore changed replacement element.");
        }

        private static ProjectElement NewOwner(string id)
        {
            var owner = new ProjectElement(id, ElementCategory.GlassWall, "F1", "L1", "Z1");
            owner.DrawingFingerprint = "drawing-a";
            owner.SourceHandles.Add("AA");
            owner.DependsOn.Add("DEP-A");
            owner.SetProperty("WidthM", "1.25");
            owner.SetProperty("GeneratedSolidHandle", "A1");
            owner.SetQuantity("GrossVolumeM3", 12.5d);
            owner.MarkClean(ElementDirtyFlags.All);
            return owner;
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("ProjectElementStateSnapshotSmoke expected " + typeof(T).Name + ".");
        }
        private static void True(bool value, string message)
        {
            if (!value) throw new InvalidOperationException("ProjectElementStateSnapshotSmoke " + message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    "ProjectElementStateSnapshotSmoke " + message +
                    " Expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
