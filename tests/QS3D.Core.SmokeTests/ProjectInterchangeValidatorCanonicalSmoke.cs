using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeValidatorCanonicalSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsPaddedProjectId();
            RejectsPaddedRelationId();
            RejectsPaddedSourceHandle();
            RejectsPaddedDependency();
            RejectsPaddedPropertyKey();
            RejectsPaddedQuantityKey();
            RejectsPaddedUnitTokens();
            RejectsTimestampWithoutOffset();
            RejectsTimestampWithExplicitOffset();
            AcceptsCanonicalUtc();
        }

        private static void RejectsPaddedProjectId() =>
            RequireIssue(Json().Replace("\"id\":\"P-VALIDATOR\"", "\"id\":\" P-VALIDATOR \""), "ID_NON_CANONICAL");

        private static void RejectsPaddedRelationId() =>
            RequireIssue(Json().Replace("\"familyId\":\"FM1\"", "\"familyId\":\" FM1 \""), "ID_NON_CANONICAL");

        private static void RejectsPaddedSourceHandle() =>
            RequireIssue(Json().Replace("\"sourceHandles\": [\"AA\"]", "\"sourceHandles\": [\" AA \"]"), "SOURCE_HANDLE_NON_CANONICAL");

        private static void RejectsPaddedDependency() =>
            RequireIssue(Json().Replace("\"dependencies\": [\"ROOT\"]", "\"dependencies\": [\" ROOT \"]"), "DEPENDENCY_NON_CANONICAL");

        private static void RejectsPaddedPropertyKey() =>
            RequireIssue(Json().Replace("\"Mark\":\"B-01\"", "\" Mark \":\"B-01\""), "PROPERTY_KEY_NON_CANONICAL");

        private static void RejectsPaddedQuantityKey() =>
            RequireIssue(Json().Replace("\"Count\":2", "\" Count \":2"), "QUANTITY_KEY_NON_CANONICAL");

        private static void RejectsPaddedUnitTokens()
        {
            RequireUnitToken("length", "m", "UNIT_LENGTH");
            RequireUnitToken("area", "m2", "UNIT_AREA");
            RequireUnitToken("volume", "m3", "UNIT_VOLUME");
            RequireUnitToken("mass", "kg", "UNIT_MASS");
        }

        private static void RequireUnitToken(string name, string value, string code)
        {
            var canonical = "\"" + name + "\":\"" + value + "\"";
            var padded = "\"" + name + "\":\" " + value + " \"";
            var json = Json().Replace(canonical, padded);
            RequireIssue(json, code);
            try
            {
                ProjectInterchangeValidatedSnapshotReader.Read(json);
            }
            catch (InvalidDataException)
            {
                return;
            }
            throw new InvalidOperationException("ProjectInterchangeValidatorCanonicalSmoke: typed reader accepted padded unit token " + name + ".");
        }

        private static void RejectsTimestampWithoutOffset() =>
            RequireIssue(Json().Replace("2026-08-10T10:00:00.0000000Z", "2026-08-10T10:00:00.0000000"), "TIMESTAMP_NOT_UTC");

        private static void RejectsTimestampWithExplicitOffset()
        {
            RequireIssue(
                Json().Replace("2026-08-10T10:00:00.0000000Z", "2026-08-10T17:00:00.0000000+07:00"),
                "TIMESTAMP_NOT_UTC");
        }

        private static void AcceptsCanonicalUtc()
        {
            var result = ProjectInterchangeJsonValidator.Validate(Json());
            if (!result.IsValid)
                throw new InvalidOperationException("ProjectInterchangeValidatorCanonicalSmoke: exact UTC round-trip timestamp was rejected: " + string.Join(",", result.Issues.Select(x => x.Code)));
        }

        private static void RequireIssue(string json, string code)
        {
            var result = ProjectInterchangeJsonValidator.Validate(json);
            if (result.IsValid || !result.Issues.Any(x => x.Code == code && x.Severity == InterchangeValidationSeverity.Error))
                throw new InvalidOperationException("ProjectInterchangeValidatorCanonicalSmoke: expected validation error " + code + ".");
        }

        private static string Json()
        {
            var project = new ProjectState("P-VALIDATOR", "Validator")
            {
                UpdatedUtc = new DateTime(2026, 8, 10, 10, 0, 0, DateTimeKind.Utc)
            };
            project.Zones.Add(new ZoneDefinition("Z1", "Zone"));
            project.Floors.Add(new FloorDefinition("F1", "Floor", 0d));
            project.Families.Add(new ProjectFamily("FM1", "Family", ElementCategory.Beam));

            var root = new ProjectElement("ROOT", ElementCategory.Beam, "FM1", "F1", "Z1");
            root.SourceHandles.Add("10");
            project.Elements.Add(root);

            var child = new ProjectElement("CHILD", ElementCategory.Beam, "FM1", "F1", "Z1");
            child.SourceHandles.Add("AA");
            child.DependsOn.Add("ROOT");
            child.SetProperty("Mark", "B-01");
            child.SetQuantity("Count", 2d);
            project.Elements.Add(child);

            project.UpdatedUtc = new DateTime(2026, 8, 10, 10, 0, 0, DateTimeKind.Utc);
            return ProjectInterchangeJsonExporter.Build(project);
        }
    }
}
