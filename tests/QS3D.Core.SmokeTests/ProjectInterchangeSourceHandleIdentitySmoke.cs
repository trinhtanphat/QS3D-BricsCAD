using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeSourceHandleIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsLeadingZeroNumericAlias();
            RejectsPrefixedNumericAlias();
            AcceptsDistinctNumericHandles();
            PreservesDistinctMalformedTextualHandles();
        }

        private static void RejectsLeadingZeroNumericAlias() =>
            RequireDuplicate(MutateHandles("A", "0A"), "leading-zero numeric alias");

        private static void RejectsPrefixedNumericAlias() =>
            RequireDuplicate(MutateHandles("A", "0x000a"), "0x/case/leading-zero numeric alias");

        private static void AcceptsDistinctNumericHandles() =>
            RequireNoDuplicate(MutateHandles("A", "B"), "distinct numeric handles");

        private static void PreservesDistinctMalformedTextualHandles() =>
            RequireNoDuplicate(MutateHandles("BAD-G", "0BAD-G"), "distinct malformed textual handles");

        private static void RequireDuplicate(string json, string scenario)
        {
            var result = ProjectInterchangeJsonValidator.Validate(json);
            if (!result.Issues.Any(x =>
                    string.Equals(x.Code, "SOURCE_HANDLE_DUPLICATE", StringComparison.Ordinal) &&
                    x.Severity == InterchangeValidationSeverity.Error))
                throw new InvalidOperationException("ProjectInterchangeSourceHandleIdentitySmoke: expected SOURCE_HANDLE_DUPLICATE for " + scenario + ".");
        }

        private static void RequireNoDuplicate(string json, string scenario)
        {
            var result = ProjectInterchangeJsonValidator.Validate(json);
            if (result.Issues.Any(x => string.Equals(x.Code, "SOURCE_HANDLE_DUPLICATE", StringComparison.Ordinal)))
                throw new InvalidOperationException("ProjectInterchangeSourceHandleIdentitySmoke: unexpected SOURCE_HANDLE_DUPLICATE for " + scenario + ".");
            if (!result.IsValid)
                throw new InvalidOperationException("ProjectInterchangeSourceHandleIdentitySmoke: otherwise-valid fixture was rejected for " + scenario + ": " + string.Join(",", result.Issues.Select(x => x.Code)));
        }

        private static string MutateHandles(string first, string second)
        {
            var json = Json();
            const string original = "\"sourceHandles\": [\"A\"]";
            var replacement = "\"sourceHandles\": [\"" + first + "\",\"" + second + "\"]";
            var mutated = json.Replace(original, replacement);
            if (string.Equals(mutated, json, StringComparison.Ordinal))
                throw new InvalidOperationException("ProjectInterchangeSourceHandleIdentitySmoke: canonical fixture did not contain the expected sourceHandles token.");
            return mutated;
        }

        private static string Json()
        {
            var project = new ProjectState("P-INTERCHANGE-HANDLE-ID", "Interchange Handle Identity")
            {
                UpdatedUtc = new DateTime(2026, 8, 12, 13, 0, 0, DateTimeKind.Utc)
            };
            project.Zones.Add(new ZoneDefinition("Z1", "Zone"));
            project.Floors.Add(new FloorDefinition("F1", "Floor", 0d));
            project.Families.Add(new ProjectFamily("FM1", "Family", ElementCategory.Beam));

            var element = new ProjectElement("E1", ElementCategory.Beam, "FM1", "F1", "Z1");
            element.SourceHandles.Add("A");
            project.Elements.Add(element);
            return ProjectInterchangeJsonExporter.Build(project);
        }
    }
}
