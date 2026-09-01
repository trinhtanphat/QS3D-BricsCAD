using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeMapDuplicateMemberSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            DuplicateFamilyPropertyFailsClosed();
            DuplicateElementPropertyFailsClosed();
            DuplicateElementQuantityFailsClosed();
            UniqueMapControlRemainsValid();
        }

        private static void DuplicateFamilyPropertyFailsClosed()
        {
            var json = ValidSnapshot().Replace("\"properties\":{\"Mark\":\"F1\"}", "\"properties\":{\"Mark\":\"F1\",\"Mark\":\"F2\"}");
            RequireDuplicate(ProjectInterchangeJsonValidator.Validate(json), "$.families[0].properties");
        }

        private static void DuplicateElementPropertyFailsClosed()
        {
            var json = ValidSnapshot().Replace("\"properties\":{\"Tag\":\"E1\"}", "\"properties\":{\"Tag\":\"E1\",\"Tag\":\"E2\"}");
            RequireDuplicate(ProjectInterchangeJsonValidator.Validate(json), "$.elements[0].properties");
        }

        private static void DuplicateElementQuantityFailsClosed()
        {
            var json = ValidSnapshot().Replace("\"quantities\":{\"LengthM\":1.0}", "\"quantities\":{\"LengthM\":1.0,\"LengthM\":2.0}");
            RequireDuplicate(ProjectInterchangeJsonValidator.Validate(json), "$.elements[0].quantities");
        }

        private static void UniqueMapControlRemainsValid()
        {
            var result = ProjectInterchangeJsonValidator.Validate(ValidSnapshot());
            if (!result.IsValid)
                throw new InvalidOperationException("Unique map control must remain valid; got " + string.Join(",", result.Issues.Select(x => x.Code)) + ".");
        }

        private static string ValidSnapshot() =>
            "{\"format\":\"QS3D.SemanticSnapshot\",\"formatVersion\":1," +
            "\"units\":{\"length\":\"m\",\"area\":\"m2\",\"volume\":\"m3\",\"mass\":\"kg\"}," +
            "\"project\":{\"id\":\"P\",\"name\":\"N\",\"schemaVersion\":1,\"drawingFingerprint\":\"\",\"updatedUtc\":\"2026-08-10T11:00:00.0000000Z\"}," +
            "\"zones\":[],\"floors\":[]," +
            "\"families\":[{\"id\":\"F1\",\"name\":\"Fam\",\"category\":\"StructuralWall\",\"properties\":{\"Mark\":\"F1\"}}]," +
            "\"elements\":[{\"id\":\"E1\",\"category\":\"StructuralWall\",\"familyId\":\"F1\",\"floorId\":\"\",\"zoneId\":\"\",\"drawingFingerprint\":\"\",\"updatedUtc\":\"2026-08-10T11:00:00.0000000Z\",\"sourceRefScope\":\"drawing-local\",\"sourceHandles\":[],\"dependencies\":[],\"properties\":{\"Tag\":\"E1\"},\"quantities\":{\"LengthM\":1.0}}]}";

        private static void RequireDuplicate(ProjectInterchangeValidationResult result, string expectedPath)
        {
            var issue = result.Issues.FirstOrDefault(x =>
                x.Severity == InterchangeValidationSeverity.Error &&
                string.Equals(x.Code, "JSON_DUPLICATE_MEMBER", StringComparison.Ordinal) &&
                string.Equals(x.Path, expectedPath, StringComparison.Ordinal));
            if (issue == null)
                throw new InvalidOperationException("Expected JSON_DUPLICATE_MEMBER @ " + expectedPath + "; got " + string.Join(",", result.Issues.Select(x => x.Code + "@" + x.Path)) + ".");
        }
    }
}
