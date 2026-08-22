using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticScheduleCatalogUpsertCanonicalIdSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalEquivalentIdReplacesExistingSchedule();
        }

        private static void CanonicalEquivalentIdReplacesExistingSchedule()
        {
            var project = new ProjectState("P-schedule-upsert", "Schedule Upsert");
            SemanticScheduleCatalog.Save(project, new[]
            {
                CreateDefinition("schedule-1", "Original", "Original title")
            });

            SemanticScheduleCatalog.Upsert(
                project,
                CreateDefinition(" SCHEDULE-1 ", "Replacement", "Replacement title"));

            var schedules = SemanticScheduleCatalog.Load(project);
            if (schedules.Count != 1)
                throw new InvalidOperationException("SemanticScheduleCatalogUpsertCanonicalIdSmoke: canonical-equivalent upsert did not keep exactly one schedule.");

            var schedule = schedules[0];
            if (!string.Equals(schedule.Id, "SCHEDULE-1", StringComparison.Ordinal))
                throw new InvalidOperationException("SemanticScheduleCatalogUpsertCanonicalIdSmoke: replacement schedule id was not persisted in trimmed canonical form.");
            if (!string.Equals(schedule.Name, "Replacement", StringComparison.Ordinal)
                || !string.Equals(schedule.Title, "Replacement title", StringComparison.Ordinal))
                throw new InvalidOperationException("SemanticScheduleCatalogUpsertCanonicalIdSmoke: canonical-equivalent upsert did not replace the existing payload.");
        }

        private static SemanticScheduleDefinition CreateDefinition(string id, string name, string title)
        {
            return new SemanticScheduleDefinition(
                id,
                name,
                title,
                Array.Empty<ElementCategory>(),
                string.Empty,
                string.Empty,
                Array.Empty<string>(),
                Array.Empty<string>(),
                new[] { new SemanticDocumentationColumn("Element", "{Id}") });
        }
    }
}
