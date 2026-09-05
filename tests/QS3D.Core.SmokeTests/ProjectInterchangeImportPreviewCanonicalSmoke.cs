using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeImportPreviewCanonicalSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ValidSnapshotStillPreviews();
            PaddedIdentityReturnsInvalidPreview();
            MissingTimezoneReturnsInvalidPreview();
        }

        private static void ValidSnapshotStillPreviews()
        {
            var target = new ProjectState("TARGET", "Target");
            var result = ProjectInterchangeImportPreview.Plan(target, Json());
            if (!result.Validation.IsValid || result.TotalIdentityCount == 0)
                throw new InvalidOperationException("ProjectInterchangeImportPreviewCanonicalSmoke: canonical snapshot did not produce a valid preview.");
        }

        private static void PaddedIdentityReturnsInvalidPreview()
        {
            var target = new ProjectState("TARGET", "Target");
            var json = Json().Replace("\"id\":\"SOURCE\"", "\"id\":\" SOURCE \"");
            var result = ProjectInterchangeImportPreview.Plan(target, json);
            if (result.Validation.IsValid || !result.Validation.Issues.Any(x => x.Code == "ID_NON_CANONICAL") || result.TotalIdentityCount != 0)
                throw new InvalidOperationException("ProjectInterchangeImportPreviewCanonicalSmoke: padded identity was not rejected by preview validation.");
        }

        private static void MissingTimezoneReturnsInvalidPreview()
        {
            var target = new ProjectState("TARGET", "Target");
            var json = Json().Replace("2026-08-10T10:00:00.0000000Z", "2026-08-10T10:00:00.0000000");
            var result = ProjectInterchangeImportPreview.Plan(target, json);
            if (result.Validation.IsValid || !result.Validation.Issues.Any(x => x.Code == "TIMESTAMP_NOT_UTC") || result.TotalIdentityCount != 0)
                throw new InvalidOperationException("ProjectInterchangeImportPreviewCanonicalSmoke: timezone-less timestamp was not rejected by preview validation.");
        }

        private static string Json()
        {
            var source = new ProjectState("SOURCE", "Source")
            {
                UpdatedUtc = new DateTime(2026, 8, 10, 10, 0, 0, DateTimeKind.Utc)
            };
            source.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));
            source.Floors.Add(new FloorDefinition("F1", "Floor 1", 0d));
            source.Families.Add(new ProjectFamily("FM1", "Beam family", ElementCategory.Beam));
            var element = new ProjectElement("E1", ElementCategory.Beam, "FM1", "F1", "Z1");
            element.SourceHandles.Add("AA");
            source.Elements.Add(element);
            source.UpdatedUtc = new DateTime(2026, 8, 10, 10, 0, 0, DateTimeKind.Utc);
            return ProjectInterchangeJsonExporter.Build(source);
        }
    }
}
