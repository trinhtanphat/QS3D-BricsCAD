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
            RelinkCollapsesLegacyDependencyVariants();
            RehostRemovesLegacyPreviousHostVariants();
            UnlinkRemovesLegacyHostVariants();
            AmbiguousPreviousHostFailsBeforeMutation();
            CanonicalRelinkIsSideEffectFree();
            MissingHostUnlinkIsSideEffectFree();
        }

        private static void RelinkCollapsesLegacyDependencyVariants()
        {
            var project = Project(out var wallA, out _, out var opening);
            opening.Properties["HostWallId"] = " wall-a ";
            opening.DependsOn.Add(" WALL-A ");
            opening.DependsOn.Add("wall-a");

            new HostLinkService().LinkOpening(project, " opening ", " wall-a ");

            Equal("WALL-A", opening.Properties["HostWallId"]);
            Equal(1, opening.DependsOn.Count);
            Equal(wallA.Id, opening.DependsOn.Single());
        }

        private static void RehostRemovesLegacyPreviousHostVariants()
        {
            var project = Project(out var wallA, out var wallB, out var opening);
            opening.Properties["HostWallId"] = " wall-a ";
            opening.DependsOn.Add(" WALL-A ");
            opening.DependsOn.Add("wall-a");

            new HostLinkService().LinkOpening(project, opening.Id, " wall-b ");

            Equal(wallB.Id, opening.Properties["HostWallId"]);
            Equal(1, opening.DependsOn.Count);
            Equal(wallB.Id, opening.DependsOn.Single());
            if (opening.DependsOn.Any(x => string.Equals(x.Trim(), wallA.Id, StringComparison.OrdinalIgnoreCase)))
                throw new Exception("Re-host must remove every legacy dependency variant of the previous wall.");
        }

        private static void UnlinkRemovesLegacyHostVariants()
        {
            var project = Project(out var wallA, out _, out var opening);
            opening.Properties["HostWallId"] = " wall-a ";
            opening.DependsOn.Add(" WALL-A ");
            opening.DependsOn.Add("wall-a");

            new HostLinkService().UnlinkOpening(project, " OPENING ");

            if (opening.Properties.ContainsKey("HostWallId")) throw new Exception("Unlink must clear HostWallId.");
            if (opening.DependsOn.Any(x => string.Equals(x.Trim(), wallA.Id, StringComparison.OrdinalIgnoreCase)))
                throw new Exception("Unlink must remove every legacy dependency variant of the host wall.");
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
