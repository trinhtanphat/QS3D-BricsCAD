using System;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedHandleOwnershipHealthSmoke
    {
        public static void Run()
        {
            SourceAndGeneratedCollisionIsReported();
            RebarAndCurtainCrossTypeCollisionIsReported();
            DuplicateWithinSameSlotIsNotCrossOwnerConflict();
            NonOwnerHandleMetadataIsIgnored();
        }

        private static void SourceAndGeneratedCollisionIsReported()
        {
            var project = new ProjectState("OWN1", "Ownership");
            var source = new ProjectElement("A", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            source.SourceHandles.Add("AA");
            var generated = new ProjectElement("B", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            generated.Properties["GeneratedSolidHandle"] = "AA";
            project.Elements.Add(source);
            project.Elements.Add(generated);
            var issues = new GeneratedHandleOwnershipHealthService().Inspect(project);
            Equal(2, Count(issues, "GENERATED_HANDLE_OWNERSHIP_CONFLICT"));
        }

        private static void RebarAndCurtainCrossTypeCollisionIsReported()
        {
            var project = new ProjectState("OWN2", "Ownership");
            var column = new ProjectElement("COL", ElementCategory.Column, string.Empty, string.Empty, string.Empty);
            column.Properties["GeneratedTieRebarHandles"] = "BC";
            var glass = new ProjectElement("GW", ElementCategory.GlassWall, string.Empty, string.Empty, string.Empty);
            glass.Properties["GeneratedCurtainFrameHandles"] = "BC";
            project.Elements.Add(column);
            project.Elements.Add(glass);
            var issues = new GeneratedHandleOwnershipHealthService().Inspect(project);
            Equal(2, Count(issues, "GENERATED_HANDLE_OWNERSHIP_CONFLICT"));
        }

        private static void DuplicateWithinSameSlotIsNotCrossOwnerConflict()
        {
            var project = new ProjectState("OWN3", "Ownership");
            var slab = new ProjectElement("SLAB", ElementCategory.Slab, string.Empty, string.Empty, string.Empty);
            slab.Properties["GeneratedSlabMeshHandles"] = "CD;CD";
            project.Elements.Add(slab);
            var issues = new GeneratedHandleOwnershipHealthService().Inspect(project);
            Equal(0, Count(issues, "GENERATED_HANDLE_OWNERSHIP_CONFLICT"));
        }

        private static void NonOwnerHandleMetadataIsIgnored()
        {
            var project = new ProjectState("OWN4", "Ownership policy");
            var first = new ProjectElement("A", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            first.Properties["PreviewHandle"] = "EF";
            var second = new ProjectElement("B", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            second.Properties["PreviewHandle"] = "EF";
            project.Elements.Add(first);
            project.Elements.Add(second);
            var issues = new GeneratedHandleOwnershipHealthService().Inspect(project);
            Equal(0, Count(issues, "GENERATED_HANDLE_OWNERSHIP_CONFLICT"));
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
