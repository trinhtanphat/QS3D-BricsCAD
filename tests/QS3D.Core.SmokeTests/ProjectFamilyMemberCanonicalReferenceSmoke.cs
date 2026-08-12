using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyMemberCanonicalReferenceSmoke
    {
        public static void Run()
        {
            PaddedRelationCountsAndBlocksDelete();
            CaseVariedPaddedRelationCounts();
            UnrelatedFamilyRemainsUnreferenced();
        }

        private static void PaddedRelationCountsAndBlocksDelete()
        {
            var project = new ProjectState("P-FAMILY-REF-1", "Family ref test");
            var family = ProjectFamilyService.Create(project, "F-BEAM", "Beam A", ElementCategory.Beam);
            var element = new ProjectElement("E-BEAM-1", ElementCategory.Beam, family.Id, string.Empty, string.Empty)
            {
                FamilyId = "  F-BEAM  "
            };
            project.Elements.Add(element);
            var beforeVersion = project.ChangeVersion;
            var beforeCount = project.Families.Count;

            Equal(1, ProjectFamilyService.ReferenceCount(project, family.Id));
            ThrowsReferenced(() => ProjectFamilyService.Delete(project, family.Id));

            Equal(beforeVersion, project.ChangeVersion);
            Equal(beforeCount, project.Families.Count);
            Same(family, project.FindFamily(family.Id));
        }

        private static void CaseVariedPaddedRelationCounts()
        {
            var project = new ProjectState("P-FAMILY-REF-2", "Family ref case test");
            var family = ProjectFamilyService.Create(project, "Fam-Column", "Column A", ElementCategory.Column);
            project.Elements.Add(new ProjectElement("E-COLUMN-1", ElementCategory.Column)
            {
                FamilyId = "  fAM-cOLUMN  "
            });

            Equal(1, ProjectFamilyService.ReferenceCount(project, " fam-COLUMN "));
        }

        private static void UnrelatedFamilyRemainsUnreferenced()
        {
            var project = new ProjectState("P-FAMILY-REF-3", "Family unrelated test");
            var referenced = ProjectFamilyService.Create(project, "F-REFERENCED", "Beam Referenced", ElementCategory.Beam);
            var unrelated = ProjectFamilyService.Create(project, "F-UNRELATED", "Beam Unrelated", ElementCategory.Beam);
            project.Elements.Add(new ProjectElement("E-REF", ElementCategory.Beam)
            {
                FamilyId = "  f-referenced  "
            });

            Equal(1, ProjectFamilyService.ReferenceCount(project, referenced.Id));
            Equal(0, ProjectFamilyService.ReferenceCount(project, unrelated.Id));
            True(ProjectFamilyService.Delete(project, unrelated.Id));
            Null(project.FindFamily(unrelated.Id));
            Same(referenced, project.FindFamily(referenced.Id));
        }

        private static void ThrowsReferenced(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (!ex.Message.Contains("is referenced by 1 semantic element(s)", StringComparison.Ordinal))
                    throw new Exception("Unexpected delete error: " + ex.Message);
                return;
            }

            throw new Exception("Expected referenced Family deletion to fail.");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void Null(object? value)
        {
            if (value != null) throw new Exception("Expected null.");
        }

        private static void Same(object expected, object? actual)
        {
            if (!ReferenceEquals(expected, actual)) throw new Exception("Expected same object reference.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
        }
    }
}
