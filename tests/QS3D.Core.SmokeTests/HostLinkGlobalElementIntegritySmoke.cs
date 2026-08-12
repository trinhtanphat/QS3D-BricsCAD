using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class HostLinkGlobalElementIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsLinkBeforeMutationOnUnrelatedDuplicateIdentity();
            RejectsUnlinkBeforeMutationOnUnrelatedDuplicateIdentity();
            AllowsValidCanonicalLink();
        }

        private static void RejectsLinkBeforeMutationOnUnrelatedDuplicateIdentity()
        {
            var project = MalformedProject("P-HOST-LINK-DUP");
            var opening = project.FindElement("OPEN")!;
            var beforeVersion = project.ChangeVersion;
            var beforeUpdated = project.UpdatedUtc;

            Throws<InvalidOperationException>(() => new HostLinkService().LinkOpening(project, "OPEN", "WALL"));

            False(opening.Properties.ContainsKey("HostWallId"), "link HostWallId");
            Equal(0, opening.DependsOn.Count, "link dependency count");
            Equal(0, project.AuditEvents.Count, "link audit count");
            Equal(beforeVersion, project.ChangeVersion, "link change version");
            Equal(beforeUpdated, project.UpdatedUtc, "link updated time");
        }

        private static void RejectsUnlinkBeforeMutationOnUnrelatedDuplicateIdentity()
        {
            var project = MalformedProject("P-HOST-UNLINK-DUP");
            var opening = project.FindElement("OPEN")!;
            opening.Properties["HostWallId"] = "WALL";
            opening.DependsOn.Add("WALL");
            var beforeVersion = project.ChangeVersion;
            var beforeUpdated = project.UpdatedUtc;

            Throws<InvalidOperationException>(() => new HostLinkService().UnlinkOpening(project, "OPEN"));

            Equal("WALL", opening.Properties["HostWallId"], "unlink HostWallId");
            Equal(1, opening.DependsOn.Count, "unlink dependency count");
            Equal("WALL", opening.DependsOn[0], "unlink dependency value");
            Equal(0, project.AuditEvents.Count, "unlink audit count");
            Equal(beforeVersion, project.ChangeVersion, "unlink change version");
            Equal(beforeUpdated, project.UpdatedUtc, "unlink updated time");
        }

        private static void AllowsValidCanonicalLink()
        {
            var project = new ProjectState("P-HOST-LINK-VALID", "Host link valid smoke");
            var wall = new ProjectElement("WALL", ElementCategory.ArchitecturalWall);
            var opening = new ProjectElement("OPEN", ElementCategory.WallOpening);
            project.Elements.Add(wall);
            project.Elements.Add(opening);
            var beforeVersion = project.ChangeVersion;

            new HostLinkService().LinkOpening(project, "OPEN", "WALL");

            Equal("WALL", opening.Properties["HostWallId"], "valid HostWallId");
            Equal(1, opening.DependsOn.Count, "valid dependency count");
            Equal("WALL", opening.DependsOn[0], "valid dependency value");
            Equal(1, project.AuditEvents.Count, "valid audit count");
            Equal(beforeVersion + 1L, project.ChangeVersion, "valid revision");
        }

        private static ProjectState MalformedProject(string id)
        {
            var project = new ProjectState(id, "Host link duplicate smoke");
            project.Elements.Add(new ProjectElement("DUP", ElementCategory.Beam));
            project.Elements.Add(new ProjectElement("dup", ElementCategory.Column));
            project.Elements.Add(new ProjectElement("WALL", ElementCategory.ArchitecturalWall));
            project.Elements.Add(new ProjectElement("OPEN", ElementCategory.WallOpening));
            return project;
        }

        private static void False(bool value, string label)
        {
            if (value) throw new Exception("HostLinkGlobalElementIntegritySmoke expected false: " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("HostLinkGlobalElementIntegritySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("HostLinkGlobalElementIntegritySmoke expected " + typeof(TException).Name + ".");
        }
    }
}
