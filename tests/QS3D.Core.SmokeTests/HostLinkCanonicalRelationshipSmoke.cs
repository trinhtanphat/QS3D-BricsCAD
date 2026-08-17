using System;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class HostLinkCanonicalRelationshipSmoke
    {
        public static void Run()
        {
            UnlinkRejectsPaddedPersistedHostIdBeforeMutation();
            LinkRejectsPaddedDependencyBeforeMutation();
            CanonicalLinkAndUnlinkRemainSupported();
        }

        private static void UnlinkRejectsPaddedPersistedHostIdBeforeMutation()
        {
            var project = NewProject(out var wall, out var opening);
            opening.Properties["HostWallId"] = " W1 ";
            opening.DependsOn.Add("W1");
            opening.MarkClean(ElementDirtyFlags.All);
            wall.MarkClean(ElementDirtyFlags.All);
            var version = project.ChangeVersion;
            var openingDirty = opening.Dirty;
            var wallDirty = wall.Dirty;

            Throws<InvalidOperationException>(() => new HostLinkService().UnlinkOpening(project, opening.Id));

            if (opening.Properties["HostWallId"] != " W1 ")
                throw new Exception("Rejected unlink must preserve malformed persisted HostWallId for repair.");
            if (opening.DependsOn.Count != 1 || opening.DependsOn[0] != "W1")
                throw new Exception("Rejected unlink must preserve dependency state.");
            if (project.ChangeVersion != version || opening.Dirty != openingDirty || wall.Dirty != wallDirty)
                throw new Exception("Rejected unlink must not mutate project or element state.");
        }

        private static void LinkRejectsPaddedDependencyBeforeMutation()
        {
            var project = NewProject(out var wall, out var opening);
            opening.DependsOn.Add(" W1 ");
            opening.MarkClean(ElementDirtyFlags.All);
            wall.MarkClean(ElementDirtyFlags.All);
            var version = project.ChangeVersion;
            var openingDirty = opening.Dirty;
            var wallDirty = wall.Dirty;

            Throws<InvalidOperationException>(() => new HostLinkService().LinkOpening(project, opening.Id, wall.Id));

            if (opening.Properties.ContainsKey("HostWallId"))
                throw new Exception("Rejected link must not synthesize HostWallId from malformed dependency state.");
            if (opening.DependsOn.Count != 1 || opening.DependsOn[0] != " W1 ")
                throw new Exception("Rejected link must preserve malformed dependency for explicit repair.");
            if (project.ChangeVersion != version || opening.Dirty != openingDirty || wall.Dirty != wallDirty)
                throw new Exception("Rejected link must not mutate project or element state.");
        }

        private static void CanonicalLinkAndUnlinkRemainSupported()
        {
            var project = NewProject(out var wall, out var opening);
            var service = new HostLinkService();

            service.LinkOpening(project, opening.Id, wall.Id);
            if (!opening.Properties.TryGetValue("HostWallId", out var hostId) || hostId != wall.Id)
                throw new Exception("Canonical link must persist exact host identity.");
            if (opening.DependsOn.Count != 1 || opening.DependsOn[0] != wall.Id)
                throw new Exception("Canonical link must persist one exact host dependency.");

            service.UnlinkOpening(project, opening.Id);
            if (opening.Properties.ContainsKey("HostWallId") || opening.DependsOn.Contains(wall.Id))
                throw new Exception("Canonical unlink must clear host relationship state.");
        }

        private static ProjectState NewProject(out ProjectElement wall, out ProjectElement opening)
        {
            var project = new ProjectState("P-HOST", "Host canonicality");
            wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            opening = new ProjectElement("O1", ElementCategory.WallOpening, string.Empty, string.Empty, string.Empty);
            project.Elements.Add(wall);
            project.Elements.Add(opening);
            return project;
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
