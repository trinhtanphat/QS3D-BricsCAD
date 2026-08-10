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
            SourceAndGeneratedCollisionStillFails();
            CrossGeneratedTypeCollisionStillFails();
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

        private static void SourceAndGeneratedCollisionStillFails()
        {
            var project = new ProjectState("OWN-SAFE-2", "Ownership source");
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
            var project = new ProjectState("OWN-SAFE-3", "Ownership generated");
            var slab = new ProjectElement("SLAB", ElementCategory.Slab, string.Empty, string.Empty, string.Empty);
            slab.Properties["GeneratedSlabMeshHandles"] = "DD";
            var glass = new ProjectElement("GLASS", ElementCategory.GlassWall, string.Empty, string.Empty, string.Empty);
            glass.Properties["GeneratedCurtainFrameHandles"] = "DD";
            project.Elements.Add(slab);
            project.Elements.Add(glass);
            Equal(2, Count(new SafeGeneratedHandleOwnershipHealthService().Inspect(project), "GENERATED_HANDLE_OWNERSHIP_CONFLICT"));
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
