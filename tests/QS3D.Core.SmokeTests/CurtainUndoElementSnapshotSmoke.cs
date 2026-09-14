using System;
using System.Collections.Generic;
using System.Reflection;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainUndoElementSnapshotSmoke
    {
        public static void Run()
        {
            RestoresExactOwnerWithoutTouchingUnrelatedElement();
            RefusesReplacementElementGenerationBeforeMutation();
        }

        private static void RestoresExactOwnerWithoutTouchingUnrelatedElement()
        {
            var project = new ProjectState("P-CURTAIN-SNAPSHOT", "Curtain snapshot");
            var owner = BuildOwner("OWNER");
            var unrelated = new ProjectElement("OTHER", ElementCategory.Room);
            unrelated.Properties["Keep"] = "before";
            project.Elements.Add(owner);
            project.Elements.Add(unrelated);
            project.Touch();

            var expectedDirty = owner.Dirty;
            var expectedUpdatedUtc = owner.UpdatedUtc;
            var projectVersion = project.ChangeVersion;
            var projectUpdatedUtc = project.UpdatedUtc;
            var snapshot = Capture(project, owner.Id);

            owner.Category = ElementCategory.ArchitecturalWall;
            owner.FamilyId = "F-MUT";
            owner.FloorId = "L-MUT";
            owner.ZoneId = "Z-MUT";
            owner.DrawingFingerprint = "drawing-mutated";
            owner.SourceHandles.Clear();
            owner.SourceHandles.Add("FF");
            owner.DependsOn.Clear();
            owner.DependsOn.Add("MUTATED-HOST");
            owner.Properties.Clear();
            owner.Properties["Mutated"] = "yes";
            owner.Quantities.Clear();
            owner.Quantities["MutatedQuantity"] = 99d;
            unrelated.Properties["Keep"] = "after";

            False(Matches(snapshot, project), "Mutated owner unexpectedly matched captured scoped snapshot.");
            Restore(snapshot, project);

            Equal(ElementCategory.GlassWall, owner.Category, "Category was not restored.");
            Equal("F-GLASS", owner.FamilyId, "FamilyId was not restored.");
            Equal("L-01", owner.FloorId, "FloorId was not restored.");
            Equal("Z-01", owner.ZoneId, "ZoneId was not restored.");
            Equal("drawing-before", owner.DrawingFingerprint, "Drawing fingerprint was not restored.");
            SequenceEqual(new[] { "A1", "B2" }, owner.SourceHandles, "Source handles were not restored.");
            SequenceEqual(new[] { "HOST-01" }, owner.DependsOn, "Dependencies were not restored.");
            Equal("3.6", owner.Properties["HeightM"], "Properties were not restored.");
            Equal("old", owner.Properties["GeneratedSolidHandle"], "Generated ownership property was not restored.");
            Equal(12.5d, owner.Quantities["GrossWallAreaM2"], "Quantity was not restored.");
            Equal(expectedDirty, owner.Dirty, "Dirty flags were not restored exactly.");
            Equal(expectedUpdatedUtc, owner.UpdatedUtc, "Element UpdatedUtc was not restored exactly.");
            Equal("after", unrelated.Properties["Keep"], "Unrelated element was overwritten.");
            Equal(projectVersion, project.ChangeVersion, "Scoped restore changed project ChangeVersion.");
            Equal(projectUpdatedUtc, project.UpdatedUtc, "Scoped restore changed project UpdatedUtc.");
            True(Matches(snapshot, project), "Restored owner does not match scoped snapshot.");
        }

        private static void RefusesReplacementElementGenerationBeforeMutation()
        {
            var project = new ProjectState("P-CURTAIN-GENERATION", "Generation fence");
            var owner = BuildOwner("OWNER");
            project.Elements.Add(owner);
            project.Touch();
            var snapshot = Capture(project, owner.Id);

            project.Elements.Remove(owner);
            var replacement = new ProjectElement("OWNER", ElementCategory.GlassWall);
            replacement.Properties["Replacement"] = "preserve";
            project.Elements.Add(replacement);

            Throws<InvalidOperationException>(() => Restore(snapshot, project));
            Equal("preserve", replacement.Properties["Replacement"], "Replacement generation was mutated before refusal.");
        }

        private static ProjectElement BuildOwner(string id)
        {
            var owner = new ProjectElement(id, ElementCategory.GlassWall, "F-GLASS", "L-01", "Z-01");
            owner.DrawingFingerprint = "drawing-before";
            owner.SourceHandles.Add("A1");
            owner.SourceHandles.Add("B2");
            owner.DependsOn.Add("HOST-01");
            owner.Properties["HeightM"] = "3.6";
            owner.Properties["GeneratedSolidHandle"] = "old";
            owner.Quantities["GrossWallAreaM2"] = 12.5d;
            owner.MarkClean(ElementDirtyFlags.All);
            owner.MarkDirty(ElementDirtyFlags.Quantity);
            return owner;
        }

        private static object Capture(ProjectState project, string id)
        {
            var method = typeof(ProjectStateSnapshot).GetMethod(
                "CaptureElement",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(ProjectState), typeof(string) },
                modifiers: null);
            if (method == null)
                throw new InvalidOperationException("ProjectStateSnapshot.CaptureElement(ProjectState,string) is missing.");
            return method.Invoke(null, new object[] { project, id })
                ?? throw new InvalidOperationException("Scoped element snapshot capture returned null.");
        }

        private static bool Matches(object snapshot, ProjectState project)
        {
            var method = snapshot.GetType().GetMethod("Matches", new[] { typeof(ProjectState) })
                ?? throw new InvalidOperationException("Scoped element snapshot Matches(ProjectState) is missing.");
            return (bool)(method.Invoke(snapshot, new object[] { project })
                ?? throw new InvalidOperationException("Scoped element snapshot Matches returned null."));
        }

        private static void Restore(object snapshot, ProjectState project)
        {
            var method = snapshot.GetType().GetMethod("Restore", new[] { typeof(ProjectState) })
                ?? throw new InvalidOperationException("Scoped element snapshot Restore(ProjectState) is missing.");
            try
            {
                method.Invoke(snapshot, new object[] { project });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        private static void SequenceEqual(IReadOnlyList<string> expected, IList<string> actual, string message)
        {
            if (expected.Count != actual.Count) throw new Exception(message);
            for (var i = 0; i < expected.Count; i++)
                if (!string.Equals(expected[i], actual[i], StringComparison.Ordinal)) throw new Exception(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void True(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }

        private static void False(bool value, string message) => True(!value, message);

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
