using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class HostLinkCanonicalRelationshipSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsPaddedPersistedHostOnUnlinkWithoutMutation();
            RejectsPaddedPersistedHostOnRelinkWithoutMutation();
            RejectsPaddedMatchingDependencyWithoutMutation();
            PreservesCanonicalRelationshipBehavior();
        }

        private static void RejectsPaddedPersistedHostOnUnlinkWithoutMutation()
        {
            var project = NewProject(out var wall, out var opening);
            opening.Properties["HostWallId"] = " W1 ";
            opening.DependsOn.Add("W1");
            var beforeVersion = project.ChangeVersion;
            var beforeDirty = opening.Dirty;

            Throws<InvalidOperationException>(() => new HostLinkService().UnlinkOpening(project, opening.Id));

            Equal(beforeVersion, project.ChangeVersion, "padded unlink project version");
            Equal(" W1 ", opening.Properties["HostWallId"], "padded unlink HostWallId");
            SequenceEqual(new[] { "W1" }, opening.DependsOn, "padded unlink dependencies");
            Equal(beforeDirty, opening.Dirty, "padded unlink dirty flags");
            Equal(0, project.AuditEvents.Count, "padded unlink audit count");
            Equal(ElementDirtyFlags.None, wall.Dirty, "padded unlink wall dirty flags");
        }

        private static void RejectsPaddedPersistedHostOnRelinkWithoutMutation()
        {
            var project = NewProject(out var wall, out var opening);
            var otherWall = new ProjectElement("W2", ElementCategory.ArchitecturalWall);
            otherWall.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(otherWall);
            opening.Properties["HostWallId"] = " W1 ";
            opening.DependsOn.Add("W1");
            var beforeVersion = project.ChangeVersion;
            var beforeDirty = opening.Dirty;

            Throws<InvalidOperationException>(() => new HostLinkService().LinkOpening(project, opening.Id, otherWall.Id));

            Equal(beforeVersion, project.ChangeVersion, "padded relink project version");
            Equal(" W1 ", opening.Properties["HostWallId"], "padded relink HostWallId");
            SequenceEqual(new[] { "W1" }, opening.DependsOn, "padded relink dependencies");
            Equal(beforeDirty, opening.Dirty, "padded relink dirty flags");
            Equal(0, project.AuditEvents.Count, "padded relink audit count");
            Equal(ElementDirtyFlags.None, wall.Dirty, "padded relink old wall dirty flags");
            Equal(ElementDirtyFlags.None, otherWall.Dirty, "padded relink new wall dirty flags");
        }

        private static void RejectsPaddedMatchingDependencyWithoutMutation()
        {
            var project = NewProject(out var wall, out var opening);
            opening.Properties["HostWallId"] = "W1";
            opening.DependsOn.Add(" W1 ");
            var beforeVersion = project.ChangeVersion;
            var beforeDirty = opening.Dirty;

            Throws<InvalidOperationException>(() => new HostLinkService().LinkOpening(project, opening.Id, wall.Id));

            Equal(beforeVersion, project.ChangeVersion, "padded dependency project version");
            Equal("W1", opening.Properties["HostWallId"], "padded dependency HostWallId");
            SequenceEqual(new[] { " W1 " }, opening.DependsOn, "padded matching dependency");
            Equal(beforeDirty, opening.Dirty, "padded dependency dirty flags");
            Equal(0, project.AuditEvents.Count, "padded dependency audit count");
            Equal(ElementDirtyFlags.None, wall.Dirty, "padded dependency wall dirty flags");
        }

        private static void PreservesCanonicalRelationshipBehavior()
        {
            var project = NewProject(out var wall, out var opening);
            opening.Properties["HostWallId"] = "W1";
            opening.DependsOn.Add("W1");
            var beforeNoOpVersion = project.ChangeVersion;

            new HostLinkService().LinkOpening(project, opening.Id, wall.Id);

            Equal(beforeNoOpVersion, project.ChangeVersion, "canonical link no-op version");
            Equal("W1", opening.Properties["HostWallId"], "canonical link HostWallId");
            SequenceEqual(new[] { "W1" }, opening.DependsOn, "canonical link dependencies");

            new HostLinkService().UnlinkOpening(project, opening.Id);

            Equal(beforeNoOpVersion + 1L, project.ChangeVersion, "canonical unlink version");
            True(!opening.Properties.ContainsKey("HostWallId"), "canonical unlink HostWallId removed");
            Equal(0, opening.DependsOn.Count, "canonical unlink dependency removed");
            Equal(1, project.AuditEvents.Count, "canonical unlink audit count");
        }

        private static ProjectState NewProject(out ProjectElement wall, out ProjectElement opening)
        {
            var project = new ProjectState("HOST-LINK-CANONICAL", "Host link canonical relationship");
            wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall);
            opening = new ProjectElement("O1", ElementCategory.WallOpening);
            wall.MarkClean(ElementDirtyFlags.All);
            opening.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(wall);
            project.Elements.Add(opening);
            return project;
        }

        private static void SequenceEqual(string[] expected, System.Collections.Generic.IEnumerable<string> actual, string label)
        {
            if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
                throw new Exception("HostLinkCanonicalRelationshipSmoke " + label + ": sequence mismatch.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("HostLinkCanonicalRelationshipSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void True(bool condition, string label)
        {
            if (!condition) throw new Exception("HostLinkCanonicalRelationshipSmoke " + label + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("HostLinkCanonicalRelationshipSmoke expected " + typeof(TException).Name + ".");
        }
    }
}
