using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyAssignmentAtomicitySmoke
    {
        public static void Run()
        {
            DuplicatePreviousFamilyBlocksWholeAssignmentBatch();
            CorruptProjectElementListBlocksPropertyPropagationBeforeMutation();
        }

        private static void DuplicatePreviousFamilyBlocksWholeAssignmentBatch()
        {
            var project = new ProjectState("family-atomic", "Family atomicity");
            var target = new ProjectFamily("TARGET", "Target", ElementCategory.Wall);
            target.Properties["ThicknessM"] = "0.3";
            var previous = new ProjectFamily("PREV", "Previous", ElementCategory.Wall);
            previous.Properties["ThicknessM"] = "0.2";
            project.Families.Add(target);
            project.Families.Add(previous);
            project.Families.Add(new ProjectFamily("DUP", "Duplicate A", ElementCategory.Wall));
            project.Families.Add(new ProjectFamily("dup", "Duplicate B", ElementCategory.Wall));

            var first = new ProjectElement("E1", ElementCategory.Wall, previous.Id, string.Empty, string.Empty);
            first.Properties["ThicknessM"] = "0.2";
            var second = new ProjectElement("E2", ElementCategory.Wall, "DUP", string.Empty, string.Empty);
            project.Elements.Add(first);
            project.Elements.Add(second);
            var beforeUpdated = project.UpdatedUtc;

            Throws<InvalidOperationException>(() => ProjectFamilyService.Assign(project, target.Id, new[] { first, second }));
            Equal(previous.Id, first.FamilyId, "First element changed family before later duplicate-family validation failed.");
            Equal("0.2", first.Properties["ThicknessM"], "First element inherited properties changed before whole batch validation completed.");
            Equal("DUP", second.FamilyId, "Second element changed despite failed batch.");
            if (project.UpdatedUtc != beforeUpdated) throw new Exception("Failed Family assignment touched project timestamp.");
        }

        private static void CorruptProjectElementListBlocksPropertyPropagationBeforeMutation()
        {
            var project = new ProjectState("family-property-atomic", "Family property atomicity");
            var family = new ProjectFamily("F1", "Family", ElementCategory.Wall);
            family.Properties["WidthM"] = "0.2";
            project.Families.Add(family);
            project.Elements.Add(null!);

            Throws<InvalidOperationException>(() => ProjectFamilyService.SetProperty(project, family.Id, "WidthM", "0.3"));
            Equal("0.2", family.Properties["WidthM"], "Family property mutated before corrupt member list validation completed.");
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal)) throw new Exception(message + " Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
