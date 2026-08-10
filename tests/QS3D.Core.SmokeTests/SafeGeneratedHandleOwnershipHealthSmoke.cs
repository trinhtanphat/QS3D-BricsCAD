using System;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SafeGeneratedHandleOwnershipHealthSmoke
    {
        public static void Run()
        {
            SharedBoundaryProvenanceIsAllowed();
            SameElementOpeningCutAliasIsAllowed();
            SourceAndGeneratedCollisionStillFails();
            CrossGeneratedTypeCollisionStillFails();
            CrossElementHostAliasCollisionStillFails();
        }

        private static void SharedBoundaryProvenanceIsAllowed()
        {
            var project = new ProjectState("OWN-SAFE-1", "Ownership provenance");
            var roomA = new ProjectElement("ROOM-A", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            var roomB = new ProjectElement("ROOM-B", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            roomA.Properties["BoundarySourceHandles"] = "AA;BB";
            roomB.Properties["BoundarySourceHandles"] = "AA;BB";
            project.Elements.Add(roomA);
            project.Elements.Add(roomB);
            var issues = new SafeGeneratedHandleOwnershipHealthService().Inspect(project);
            Equal(0, Count(issues, "GENERATED_HANDLE_OWNERSHIP_CONFLICT"));
        }

        private static void SameElementOpeningCutAliasIsAllowed()
        {
            var project = new ProjectState("OWN-SAFE-2", "Ownership host alias");
            var wall = new ProjectElement("WALL", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            wall.Properties["GeneratedSolidHandle"] = "ABCD";
            wall.Properties["PhysicalOpeningCutSolidHandle"] = "ABCD";
            project.Elements.Add(wall);

            Equal(0, Count(new SafeGeneratedHandleOwnershipHealthService().Inspect(project), "GENERATED_HANDLE_OWNERSHIP_CONFLICT"));
            if (!GeneratedHandleOwnershipPolicy.TryFindOwner(project, "ABCD", out var owner, out _))
                throw new Exception("Expected host solid alias to resolve to its semantic owner.");
            if (owner == null || !string.Equals(owner.Id, "WALL", StringComparison.Ordinal))
                throw new Exception("Host solid alias resolved to the wrong semantic owner.");
        }

        private static void SourceAndGeneratedCollisionStillFails()
        {
            var project = new ProjectState("OWN-SAFE-3", "Ownership source");
            var source = new ProjectElement("WALL", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            source.SourceHandles.Add("CC");
            var generated = new ProjectElement("BEAM", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            generated.Properties["GeneratedSolidHandle"] = "CC";
            project.Elements.Add(source);
            project.Elements.Add(generated);
            Equal(2, Count(new SafeGeneratedHandleOwnershipHealthService().Inspect(project), "GENERATED_HANDLE_OWNERSHIP_CONFLICT"));
        }

        private static void CrossGeneratedTypeCollisionStillFails()
        {
            var project = new ProjectState("OWN-SAFE-4", "Ownership generated");
            var slab = new ProjectElement("SLAB", ElementCategory.Slab, string.Empty, string.Empty, string.Empty);
            slab.Properties["GeneratedSlabMeshHandles"] = "DD";
            var glass = new ProjectElement("GLASS", ElementCategory.GlassWall, string.Empty, string.Empty, string.Empty);
            glass.Properties["GeneratedCurtainFrameHandles"] = "DD";
            project.Elements.Add(slab);
            project.Elements.Add(glass);
            Equal(2, Count(new SafeGeneratedHandleOwnershipHealthService().Inspect(project), "GENERATED_HANDLE_OWNERSHIP_CONFLICT"));
        }

        private static void CrossElementHostAliasCollisionStillFails()
        {
            var project = new ProjectState("OWN-SAFE-5", "Ownership host alias collision");
            var wallA = new ProjectElement("WALL-A", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            wallA.Properties["GeneratedSolidHandle"] = "EE";
            var wallB = new ProjectElement("WALL-B", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            wallB.Properties["PhysicalOpeningCutSolidHandle"] = "EE";
            project.Elements.Add(wallA);
            project.Elements.Add(wallB);

            Equal(2, Count(new SafeGeneratedHandleOwnershipHealthService().Inspect(project), "GENERATED_HANDLE_OWNERSHIP_CONFLICT"));
            var threw = false;
            try
            {
                GeneratedHandleOwnershipPolicy.TryFindOwner(project, "EE", out _, out _);
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            if (!threw) throw new Exception("Cross-element host alias collision must remain ambiguous.");
        }

        private static int Count(System.Collections.Generic.IReadOnlyList<ModelHealthIssue> issues, string code)
        {
            var count = 0;
            foreach (var issue in issues)
                if (string.Equals(issue.Code, code, StringComparison.Ordinal)) count++;
            return count;
        }

        private static void Equal(int expected, int actual)
        {
            if (expected != actual) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }
}
