using System;
using System.IO;
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
            PaddedIdentityFailsBeforePreview();
            MissingTimezoneFailsBeforePreview();
        }

        private static void ValidSnapshotStillPreviews()
        {
            var target = new ProjectState("TARGET", "Target");
            var result = ProjectInterchangeImportPreview.Plan(target, Json());
            if (!result.Validation.IsValid || result.TotalIdentityCount == 0)
                throw new InvalidOperationException("ProjectInterchangeImportPreviewCanonicalSmoke: canonical snapshot did not produce a valid preview.");
        }

        private static void PaddedIdentityFailsBeforePreview()
        {
            var target = new ProjectState("TARGET", "Target");
            var json = Json().Replace("\"id\":\"SOURCE\"", "\"id\":\" SOURCE \"");
            Throws<InvalidDataException>(() => ProjectInterchangeImportPreview.Plan(target, json));
        }

        private static void MissingTimezoneFailsBeforePreview()
        {
            var target = new ProjectState("TARGET", "Target");
            var json = Json().Replace("2026-08-10T10:00:00.0000000Z", "2026-08-10T10:00:00.0000000");
            Throws<InvalidDataException>(() => ProjectInterchangeImportPreview.Plan(target, json));
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
            return ProjectInterchangeJsonExporter.Build(source);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("ProjectInterchangeImportPreviewCanonicalSmoke expected " + typeof(T).Name + ".");
        }
    }
}
