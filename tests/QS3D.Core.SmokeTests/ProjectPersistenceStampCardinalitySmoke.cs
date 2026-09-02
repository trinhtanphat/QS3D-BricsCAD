using System;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectPersistenceStampCardinalitySmoke
    {
        private const int AboveLegacyTopLevelLimit = 10001;
        private const int AboveNestedLimit = 10001;

        public static void Run()
        {
            ValidTopLevelCardinalityAboveLegacyStampLimitIsAccepted();
            NestedCardinalityAboveQsdbLimitRemainsRejected();
        }

        private static void ValidTopLevelCardinalityAboveLegacyStampLimitIsAccepted()
        {
            var project = new ProjectState("stamp-cardinality-top-level", "Stamp cardinality");
            for (var index = 0; index < AboveLegacyTopLevelLimit; index++)
                project.Elements.Add(new ProjectElement("E-" + index.ToString("D5"), ElementCategory.Wall));

            ProjectPersistenceStamp stamp;
            try
            {
                stamp = new ProjectPersistenceStamp(project);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(
                    "Persistence stamp rejected a top-level element count that the canonical QSDB structural contract admits.",
                    ex);
            }

            if (stamp.RequiresSave(project))
                throw new InvalidOperationException("A newly captured persistence stamp unexpectedly reports the unchanged large project as dirty.");
        }

        private static void NestedCardinalityAboveQsdbLimitRemainsRejected()
        {
            var project = new ProjectState("stamp-cardinality-nested", "Nested cardinality");
            var family = new ProjectFamily("F-01", "Large property family", ElementCategory.Wall);
            project.Families.Add(family);
            for (var index = 0; index < AboveNestedLimit; index++)
                family.Properties.Add("P-" + index.ToString("D5"), "value");

            try
            {
                _ = new ProjectPersistenceStamp(project);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("supports at most 10000 entries", StringComparison.Ordinal) >= 0)
                    return;
                throw new InvalidOperationException("Nested cardinality failed for an unexpected reason.", ex);
            }

            throw new InvalidOperationException("Persistence stamp accepted nested family property cardinality above the canonical QSDB limit.");
        }
    }
}
