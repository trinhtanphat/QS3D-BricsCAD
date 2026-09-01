using System;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectStateSnapshotRelationIdentitySmoke
    {
        public static void Run()
        {
            RejectsNonCanonicalSourceHandles();
            RejectsNonCanonicalDependencies();
            PreservesCanonicalUnicodeRelations();
        }

        private static void RejectsNonCanonicalSourceHandles()
        {
            ExpectRejectedRelation(true, " A1 ", "padded source handle");
            ExpectRejectedRelation(true, "   ", "blank source handle");
            ExpectRejectedRelation(true, "A\t1", "control-bearing source handle");
            ExpectRejectedRelation(true, "A\uD8001", "malformed source handle");
            ExpectRejectedDuplicate(true, "A1", "a1", "case-insensitive duplicate source handle");
        }

        private static void RejectsNonCanonicalDependencies()
        {
            ExpectRejectedRelation(false, " HOST ", "padded dependency");
            ExpectRejectedRelation(false, "\t", "blank dependency");
            ExpectRejectedRelation(false, "HOST\n1", "control-bearing dependency");
            ExpectRejectedRelation(false, "HOST\uD800", "malformed dependency");
            ExpectRejectedDuplicate(false, "HOST", "host", "case-insensitive duplicate dependency");
        }

        private static void ExpectRejectedRelation(bool sourceHandle, string value, string label)
        {
            var project = NewProject(label);
            var element = project.Elements[0];
            var values = sourceHandle ? element.SourceHandles : element.DependsOn;
            values.Add(value);
            var originalDirty = element.Dirty;
            var originalUpdatedUtc = element.UpdatedUtc;
            var originalChangeVersion = project.ChangeVersion;
            var originalProjectUpdatedUtc = project.UpdatedUtc;

            ExpectInvalidOperation(() => ProjectStateSnapshot.Capture(project), label + " was accepted by snapshot capture.");
            ExpectInvalidOperation(() => ProjectStateSnapshot.CreateDetachedCopy(project), label + " was accepted by detached copy.");

            Require(values.Count == 1 && string.Equals(values[0], value, StringComparison.Ordinal), label + " rejection mutated relation source state.");
            Require(element.Dirty == originalDirty && element.UpdatedUtc == originalUpdatedUtc, label + " rejection changed element persistence state.");
            Require(project.ChangeVersion == originalChangeVersion && project.UpdatedUtc == originalProjectUpdatedUtc, label + " rejection changed project persistence state.");
        }

        private static void ExpectRejectedDuplicate(bool sourceHandle, string first, string second, string label)
        {
            var project = NewProject(label);
            var values = sourceHandle ? project.Elements[0].SourceHandles : project.Elements[0].DependsOn;
            values.Add(first);
            values.Add(second);
            ExpectInvalidOperation(() => ProjectStateSnapshot.Capture(project), label + " was accepted by snapshot capture.");
            ExpectInvalidOperation(() => ProjectStateSnapshot.CreateDetachedCopy(project), label + " was accepted by detached copy.");
            Require(values.Count == 2 && values[0] == first && values[1] == second, label + " rejection mutated relation source state.");
        }

        private static void PreservesCanonicalUnicodeRelations()
        {
            var project = NewProject("canonical-unicode");
            var element = project.Elements[0];
            const string handle = "HANDLE-\U0001F680";
            const string dependency = "HOST-\U0001F680";
            element.SourceHandles.Add(handle);
            element.DependsOn.Add(dependency);

            var detached = ProjectStateSnapshot.CreateDetachedCopy(project);
            var copy = detached.FindElement("E1") ?? throw new Exception("Detached snapshot lost the canonical relation fixture element.");
            Require(copy.SourceHandles.Count == 1 && string.Equals(copy.SourceHandles[0], handle, StringComparison.Ordinal), "Detached snapshot changed canonical source-handle text.");
            Require(copy.DependsOn.Count == 1 && string.Equals(copy.DependsOn[0], dependency, StringComparison.Ordinal), "Detached snapshot changed canonical dependency text.");
        }

        private static ProjectState NewProject(string label)
        {
            var project = new ProjectState("snapshot-relation-" + label.Replace(" ", "-"), "Snapshot relation fixture");
            var element = new ProjectElement("E1", ElementCategory.Room);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            return project;
        }

        private static void ExpectInvalidOperation(Action action, string message)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }
            throw new Exception(message);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}
