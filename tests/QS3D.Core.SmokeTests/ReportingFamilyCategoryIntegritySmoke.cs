using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class ReportingFamilyCategoryIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            MismatchedFamilyCategoryFailsClosed();
            MatchingFamilyCategoryRemainsValid();
            BlankFamilyReferenceRemainsValid();
        }

        private static void MismatchedFamilyCategoryFailsClosed()
        {
            var project = new ProjectState("report-family-mismatch", "Reporting Family category mismatch");
            var family = new ProjectFamily("Family-A", "Beam Family", ElementCategory.Beam);
            family.Properties["Material"] = "WRONG-MATERIAL";
            project.Families.Add(family);
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Slab, "family-a", string.Empty, string.Empty));

            AssertSharedBuildersReject(project, "category Slab does not match family 'Family-A' category Beam");
        }

        private static void MatchingFamilyCategoryRemainsValid()
        {
            var project = new ProjectState("report-family-match", "Reporting matching Family category");
            var family = new ProjectFamily("Family-A", "Slab Family", ElementCategory.Slab);
            family.Properties["Material"] = "Concrete";
            project.Families.Add(family);
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Slab, "family-a", string.Empty, string.Empty));

            var quantity = ProjectQuantityReportBuilder.Group(project);
            if (quantity.Count != 1 || quantity[0].FamilyName != "Slab Family" || quantity[0].Material != "Concrete")
                throw new InvalidOperationException("Matching Family/category reporting no longer preserves inherited metadata.");

            var material = MaterialUsageScheduleBuilder.Build(project);
            if (material.Count != 1 || material[0].FamilyName != "Slab Family" || material[0].MaterialName != "Concrete")
                throw new InvalidOperationException("Matching Family/category material usage no longer preserves inherited metadata.");
        }

        private static void BlankFamilyReferenceRemainsValid()
        {
            var project = new ProjectState("report-family-blank", "Reporting blank Family");
            var element = new ProjectElement("E1", ElementCategory.Slab);
            element.Properties["Material"] = "Instance Material";
            project.Elements.Add(element);

            var quantity = ProjectQuantityReportBuilder.Group(project);
            if (quantity.Count != 1 || quantity[0].Material != "Instance Material")
                throw new InvalidOperationException("Blank Family reporting no longer preserves instance metadata.");

            var material = MaterialUsageScheduleBuilder.Build(project);
            if (material.Count != 1 || material[0].MaterialName != "Instance Material")
                throw new InvalidOperationException("Blank Family material usage no longer preserves instance metadata.");
        }

        private static void AssertSharedBuildersReject(ProjectState project, string expectedMessage)
        {
            ExpectInvalid(() => MaterialUsageScheduleBuilder.Build(project), expectedMessage);
            ExpectInvalid(() => ProjectQuantityReportBuilder.Group(project), expectedMessage);
            ExpectInvalid(() => ProjectQuantityReportBuilder.Detail(project), expectedMessage);
        }

        private static void ExpectInvalid(Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedMessage, StringComparison.Ordinal) >= 0) return;
                throw new InvalidOperationException("Reporting rejected the Family/category mismatch for an unexpected reason: " + ex.Message, ex);
            }

            throw new InvalidOperationException("Expected reporting to reject a Family/category mismatch: " + expectedMessage + ".");
        }
    }
}
