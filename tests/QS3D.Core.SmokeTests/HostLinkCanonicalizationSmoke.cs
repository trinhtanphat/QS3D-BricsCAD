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

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }
}
