using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkFamilyPreviousCategoryIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            MismatchedPreviousFamilyCategoryFailsClosed();
            MatchingPreviousFamilyCategoryStillReassigns();
        }

        private static void MismatchedPreviousFamilyCategoryFailsClosed()
        {
            var project = new ProjectState("P-BULK-PREV-CATEGORY-1", "Bulk previous Family category integrity");
            var wrongPrevious = new ProjectFamily("F-DOOR-PREV", "Wrong previous door Family", ElementCategory.Door);
            wrongPrevious.Properties["LegacyInherited"] = "from-wrong-family";
            var target = new ProjectFamily("F-BEAM-TARGET", "Beam target Family", ElementCategory.Beam);
            target.Properties["WidthM"] = "0.4";
            project.Families.Add(wrongPrevious);
            project.Families.Add(target);

            var element = new ProjectElement("E-BEAM-1", ElementCategory.Beam, wrongPrevious.Id, "F1", "Z1");
            element.Properties["LegacyInherited"] = "from-wrong-family";
            element.Properties["KeepInstance"] = "keep";
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);

            var beforeVersion = project.ChangeVersion;
            var beforeUpdated = element.UpdatedUtc;
            var beforeFamilyId = element.FamilyId;
            var beforeLegacy = element.Properties["LegacyInherited"];
            var beforeKeep = element.Properties["KeepInstance"];

            ThrowsContaining<InvalidOperationException>(
                () => new BulkEditService().AssignFamily(project, new[] { element.Id }, target.Id),
                "references previous Family 'F-DOOR-PREV' category Door while the element category is Beam");

            Equal(beforeVersion, project.ChangeVersion, "mismatch project revision");
            Equal(beforeUpdated, element.UpdatedUtc, "mismatch element timestamp");
            Equal(beforeFamilyId, element.FamilyId, "mismatch FamilyId");
            Equal(beforeLegacy, element.Properties["LegacyInherited"], "mismatch legacy property");
            Equal(beforeKeep, element.Properties["KeepInstance"], "mismatch instance property");
            False(element.Properties.ContainsKey("WidthM"), "mismatch target property");
            Equal(ElementDirtyFlags.None, element.Dirty, "mismatch dirty flags");
        }

        private static void MatchingPreviousFamilyCategoryStillReassigns()
        {
            var project = new ProjectState("P-BULK-PREV-CATEGORY-2", "Bulk previous Family category valid path");
            var previous = new ProjectFamily("F-BEAM-PREV", "Previous beam Family", ElementCategory.Beam);
            previous.Properties["WidthM"] = "0.3";
            previous.Properties["LegacyInherited"] = "old";
            var target = new ProjectFamily("F-BEAM-NEXT", "Next beam Family", ElementCategory.Beam);
            target.Properties["WidthM"] = "0.5";
            project.Families.Add(previous);
            project.Families.Add(target);

            var element = new ProjectElement("E-BEAM-2", ElementCategory.Beam, previous.Id, "F1", "Z1");
            element.Properties["WidthM"] = "0.3";
            element.Properties["LegacyInherited"] = "old";
            element.Properties["KeepInstance"] = "keep";
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            var beforeVersion = project.ChangeVersion;

            var changed = new BulkEditService().AssignFamily(project, new[] { element.Id }, target.Id);

            Equal(1, changed, "valid assignment count");
            Equal(target.Id, element.FamilyId, "valid FamilyId");
            Equal("0.5", element.Properties["WidthM"], "valid target inherited property");
            False(element.Properties.ContainsKey("LegacyInherited"), "valid obsolete inherited property");
            Equal("keep", element.Properties["KeepInstance"], "valid instance override");
            Equal(beforeVersion + 1L, project.ChangeVersion, "valid project revision");
        }

        private static void False(bool value, string label)
        {
            if (value) throw new Exception("BulkFamilyPreviousCategoryIntegritySmoke expected false: " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("BulkFamilyPreviousCategoryIntegritySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void ThrowsContaining<TException>(Action action, string expectedText) where TException : Exception
        {
            try { action(); }
            catch (TException ex)
            {
                if (ex.Message.IndexOf(expectedText, StringComparison.Ordinal) >= 0) return;
                throw new Exception("BulkFamilyPreviousCategoryIntegritySmoke expected message containing '" + expectedText + "', actual='" + ex.Message + "'.");
            }
            throw new Exception("BulkFamilyPreviousCategoryIntegritySmoke expected " + typeof(TException).Name + ".");
        }
    }
}
