using System;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkFamilyRelationDirtySmoke
    {
        public static void Run()
        {
            GeneratedGeometryFamilyChangeMarksAllSemanticFreshness();
            NonGeneratedFamilyChangeMarksRelationsWithoutGeometry();
            CanonicalSameFamilyRemainsNoOp();
        }

        private static void GeneratedGeometryFamilyChangeMarksAllSemanticFreshness()
        {
            var project = new ProjectState("P-BULK-FAMILY-DIRTY-WALL", "Bulk family dirty wall");
            var previous = new ProjectFamily("F-OLD", "Old wall", ElementCategory.ArchitecturalWall);
            previous.Properties["ThicknessM"] = "0.20";
            var target = new ProjectFamily("F-NEW", "New wall", ElementCategory.ArchitecturalWall);
            target.Properties["ThicknessM"] = "0.30";
            project.Families.Add(previous);
            project.Families.Add(target);

            var element = new ProjectElement("E1", ElementCategory.ArchitecturalWall, previous.Id, string.Empty, string.Empty);
            element.Properties["ThicknessM"] = previous.Properties["ThicknessM"];
            project.Elements.Add(element);
            element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;

            var changed = new BulkEditService().AssignFamily(project, new[] { element.Id }, target.Id);

            if (changed != 1) throw new Exception("Bulk Family reassignment did not report exactly one changed element.");
            Equal(target.Id, element.FamilyId, "Bulk Family reassignment did not update FamilyId.");
            RequireFlags(element.Dirty, ElementDirtyFlags.Properties | ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity | ElementDirtyFlags.Geometry,
                "Generated-geometry Family reassignment did not mark complete semantic/geometry freshness.");
            if (project.ChangeVersion != beforeVersion + 1L)
                throw new Exception("Bulk Family reassignment should touch project ChangeVersion exactly once.");
        }

        private static void NonGeneratedFamilyChangeMarksRelationsWithoutGeometry()
        {
            var project = new ProjectState("P-BULK-FAMILY-DIRTY-ROOM", "Bulk family dirty room");
            var previous = new ProjectFamily("R-OLD", "Old room", ElementCategory.Room);
            var target = new ProjectFamily("R-NEW", "New room", ElementCategory.Room);
            project.Families.Add(previous);
            project.Families.Add(target);

            var element = new ProjectElement("R1", ElementCategory.Room, previous.Id, string.Empty, string.Empty);
            project.Elements.Add(element);
            element.MarkClean(ElementDirtyFlags.All);

            new BulkEditService().AssignFamily(project, new[] { element.Id }, target.Id);

            RequireFlags(element.Dirty, ElementDirtyFlags.Properties | ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity,
                "Non-generated Family reassignment did not mark relation/property/quantity freshness.");
            if ((element.Dirty & ElementDirtyFlags.Geometry) != 0)
                throw new Exception("Non-generated Family reassignment introduced unnecessary Geometry dirty state.");
        }

        private static void CanonicalSameFamilyRemainsNoOp()
        {
            var project = new ProjectState("P-BULK-FAMILY-DIRTY-NOOP", "Bulk family no-op");
            var target = new ProjectFamily("F-TARGET", "Target", ElementCategory.Room);
            project.Families.Add(target);
            var element = new ProjectElement("E1", ElementCategory.Room, target.Id, string.Empty, string.Empty);
            project.Elements.Add(element);
            element.FamilyId = "  f-target  ";
            element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = element.UpdatedUtc;
            var beforeFamilyId = element.FamilyId;

            var changed = new BulkEditService().AssignFamily(project, new[] { element.Id }, target.Id);

            if (changed != 0) throw new Exception("Canonical same-Family assignment was reported as a mutation.");
            if (project.ChangeVersion != beforeVersion) throw new Exception("Canonical same-Family assignment touched project freshness.");
            if (element.Dirty != ElementDirtyFlags.None || element.UpdatedUtc != beforeUpdatedUtc)
                throw new Exception("Canonical same-Family assignment dirtied element freshness.");
            Equal(beforeFamilyId, element.FamilyId, "Canonical same-Family assignment rewrote the raw no-op relation.");
        }

        private static void RequireFlags(ElementDirtyFlags actual, ElementDirtyFlags required, string message)
        {
            if ((actual & required) != required)
                throw new Exception(message + " Required=" + required + ", actual=" + actual + ".");
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception(message + " Expected='" + expected + "', actual='" + actual + "'.");
        }
    }
}
