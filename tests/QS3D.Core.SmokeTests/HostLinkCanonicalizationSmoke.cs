using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class HostLinkCanonicalizationSmoke
    {
        public static void Run()
        {
            NonCanonicalRelinkFailsBeforeMutation();
            NonCanonicalRehostFailsBeforeMutation();
            NonCanonicalUnlinkFailsBeforeMutation();
            AmbiguousPreviousHostFailsBeforeMutation();
            CanonicalRelinkIsSideEffectFree();
            MissingHostUnlinkIsSideEffectFree();
            BlankHostMetadataUnlinkFailsBeforeMutation();
            AuditedHostMutationsAdvanceRevisionOnce();
            StaleAutoHostCleanupAdvancesRevisionOnce();
        }

        private static void NonCanonicalRelinkFailsBeforeMutation()
        {
            var project = Project(out var wallA, out _, out var opening);
            opening.Properties["HostWallId"] = " wall-a ";
            opening.DependsOn.Add(" WALL-A ");
            opening.DependsOn.Add("wall-a");
            var version = project.ChangeVersion;
            var audits = project.AuditEvents.Count;

            Throws<InvalidOperationException>(() => new HostLinkService().LinkOpening(project, " opening ", " wall-a "));

            Equal(" wall-a ", opening.Properties["HostWallId"]);
            Equal(2, opening.DependsOn.Count);
            Equal(" WALL-A ", opening.DependsOn[0]);
            Equal("wall-a", opening.DependsOn[1]);
            Equal(version, project.ChangeVersion);
            Equal(audits, project.AuditEvents.Count);
            Equal(ElementDirtyFlags.All, opening.Dirty);
            Equal(ElementDirtyFlags.All, wallA.Dirty);
        }

        private static void NonCanonicalRehostFailsBeforeMutation()
        {
            var project = Project(out var wallA, out var wallB, out var opening);
            opening.Properties["HostWallId"] = " wall-a ";
            opening.DependsOn.Add(" WALL-A ");
            opening.DependsOn.Add("wall-a");
            var version = project.ChangeVersion;
            var audits = project.AuditEvents.Count;

            Throws<InvalidOperationException>(() => new HostLinkService().LinkOpening(project, opening.Id, " wall-b "));

            Equal(" wall-a ", opening.Properties["HostWallId"]);
            Equal(2, opening.DependsOn.Count);
            Equal(" WALL-A ", opening.DependsOn[0]);
            Equal("wall-a", opening.DependsOn[1]);
            Equal(version, project.ChangeVersion);
            Equal(audits, project.AuditEvents.Count);
            Equal(ElementDirtyFlags.All, opening.Dirty);
            Equal(ElementDirtyFlags.All, wallA.Dirty);
            Equal(ElementDirtyFlags.All, wallB.Dirty);
        }

        private static void NonCanonicalUnlinkFailsBeforeMutation()
        {
            var project = Project(out var wallA, out _, out var opening);
            opening.Properties["HostWallId"] = " wall-a ";
            opening.DependsOn.Add(" WALL-A ");
            opening.DependsOn.Add("wall-a");
            var version = project.ChangeVersion;
            var audits = project.AuditEvents.Count;

            Throws<InvalidOperationException>(() => new HostLinkService().UnlinkOpening(project, " OPENING "));

            Equal(" wall-a ", opening.Properties["HostWallId"]);
            Equal(2, opening.DependsOn.Count);
            Equal(" WALL-A ", opening.DependsOn[0]);
            Equal("wall-a", opening.DependsOn[1]);
            Equal(version, project.ChangeVersion);
            Equal(audits, project.AuditEvents.Count);
            Equal(ElementDirtyFlags.All, opening.Dirty);
            Equal(ElementDirtyFlags.All, wallA.Dirty);
        }

        private static void AmbiguousPreviousHostFailsBeforeMutation()
        {
            var rehostProject = AmbiguousProject(out var rehostOpening);
            Throws<InvalidOperationException>(() => new HostLinkService().LinkOpening(rehostProject, rehostOpening.Id, "WALL-B"));
            Equal("WALL-A", rehostOpening.Properties["HostWallId"]);
            Equal(1, rehostOpening.DependsOn.Count);
            Equal("WALL-A", rehostOpening.DependsOn.Single());

            var unlinkProject = AmbiguousProject(out var unlinkOpening);
            Throws<InvalidOperationException>(() => new HostLinkService().UnlinkOpening(unlinkProject, unlinkOpening.Id));
            Equal("WALL-A", unlinkOpening.Properties["HostWallId"]);
            Equal(1, unlinkOpening.DependsOn.Count);
            Equal("WALL-A", unlinkOpening.DependsOn.Single());
        }

        private static void CanonicalRelinkIsSideEffectFree()
        {
            var project = Project(out var wallA, out _, out var opening);
            opening.Properties["HostWallId"] = wallA.Id;
            opening.DependsOn.Add(wallA.Id);
            opening.MarkClean(ElementDirtyFlags.All);
            wallA.MarkClean(ElementDirtyFlags.All);
            var version = project.ChangeVersion;
            var audits = project.AuditEvents.Count;

            new HostLinkService().LinkOpening(project, opening.Id, wallA.Id);

            Equal(version, project.ChangeVersion);
            Equal(audits, project.AuditEvents.Count);
            Equal(ElementDirtyFlags.None, opening.Dirty);
            Equal(ElementDirtyFlags.None, wallA.Dirty);
            Equal(1, opening.DependsOn.Count);
            Equal(wallA.Id, opening.DependsOn.Single());
        }

        private static void MissingHostUnlinkIsSideEffectFree()
        {
            var project = Project(out _, out var wallB, out var opening);
            opening.DependsOn.Add(wallB.Id);
            opening.MarkClean(ElementDirtyFlags.All);
            wallB.MarkClean(ElementDirtyFlags.All);
            var version = project.ChangeVersion;
            var audits = project.AuditEvents.Count;

            new HostLinkService().UnlinkOpening(project, opening.Id);

            Equal(version, project.ChangeVersion);
            Equal(audits, project.AuditEvents.Count);
            Equal(ElementDirtyFlags.None, opening.Dirty);
            Equal(ElementDirtyFlags.None, wallB.Dirty);
            Equal(1, opening.DependsOn.Count);
            Equal(wallB.Id, opening.DependsOn.Single());
        }

        private static void BlankHostMetadataUnlinkFailsBeforeMutation()
        {
            var project = Project(out var wallA, out _, out var opening);
            opening.Properties["HostWallId"] = "   ";
            opening.Properties["AutoHostMatched"] = "true";
            opening.DependsOn.Add(wallA.Id);
            opening.MarkClean(ElementDirtyFlags.All);
            wallA.MarkClean(ElementDirtyFlags.All);
            var version = project.ChangeVersion;
            var audits = project.AuditEvents.Count;

            Throws<InvalidOperationException>(() => new HostLinkService().UnlinkOpening(project, opening.Id));

            Equal("   ", opening.Properties["HostWallId"]);
            Equal("true", opening.Properties["AutoHostMatched"]);
            Equal(1, opening.DependsOn.Count);
            Equal(wallA.Id, opening.DependsOn.Single());
            Equal(version, project.ChangeVersion);
            Equal(audits, project.AuditEvents.Count);
            Equal(ElementDirtyFlags.None, opening.Dirty);
            Equal(ElementDirtyFlags.None, wallA.Dirty);
        }

        private static void AuditedHostMutationsAdvanceRevisionOnce()
        {
            var project = Project(out var wallA, out _, out var opening);
            var service = new HostLinkService();
            var beforeLinkVersion = project.ChangeVersion;
            var beforeLinkAudits = project.AuditEvents.Count;
            service.LinkOpening(project, opening.Id, wallA.Id);
            Equal(beforeLinkVersion + 1L, project.ChangeVersion);
            Equal(beforeLinkAudits + 1, project.AuditEvents.Count);
            Equal("host.link", project.AuditEvents.Last().Action);
            var beforeUnlinkVersion = project.ChangeVersion;
            var beforeUnlinkAudits = project.AuditEvents.Count;
            service.UnlinkOpening(project, opening.Id);
            Equal(beforeUnlinkVersion + 1L, project.ChangeVersion);
            Equal(beforeUnlinkAudits + 1, project.AuditEvents.Count);
            Equal("host.unlink", project.AuditEvents.Last().Action);
        }

        private static void StaleAutoHostCleanupAdvancesRevisionOnce()
        {
            var project = Project(out _, out _, out var opening);
            opening.Properties["AutoHostMatched"] = "true";
            opening.Properties["AutoHostGapM"] = "0.01";
            var beforeVersion = project.ChangeVersion;
            var beforeAudits = project.AuditEvents.Count;
            new HostLinkService().UnlinkOpening(project, opening.Id);
            Equal(beforeVersion + 1L, project.ChangeVersion);
            Equal(beforeAudits + 1, project.AuditEvents.Count);
            Equal("host.auto-provenance.clear", project.AuditEvents.Last().Action);
            if (opening.Properties.ContainsKey("AutoHostMatched") || opening.Properties.ContainsKey("AutoHostGapM"))
                throw new Exception("Stale AutoHost provenance cleanup did not clear legacy metadata.");
        }

        private static ProjectState Project(out ProjectElement wallA, out ProjectElement wallB, out ProjectElement opening)
        {
            var project = new ProjectState("P1", "Host links");
            wallA = new ProjectElement("WALL-A", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            wallB = new ProjectElement("WALL-B", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            opening = new ProjectElement("OPENING", ElementCategory.WallOpening, string.Empty, string.Empty, string.Empty);
            project.Elements.Add(wallA);
            project.Elements.Add(wallB);
            project.Elements.Add(opening);
            return project;
        }

        private static ProjectState AmbiguousProject(out ProjectElement opening)
        {
            var project = new ProjectState("P-AMBIGUOUS", "Ambiguous host links");
            project.Elements.Add(new ProjectElement("WALL-A", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty));
            project.Elements.Add(new ProjectElement("wall-a", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty));
            project.Elements.Add(new ProjectElement("WALL-B", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty));
            opening = new ProjectElement("OPENING", ElementCategory.WallOpening, string.Empty, string.Empty, string.Empty);
            opening.Properties["HostWallId"] = "WALL-A";
            opening.DependsOn.Add("WALL-A");
            project.Elements.Add(opening);
            return project;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
