using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeDuplicateMemberSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            DuplicateRootMemberFailsClosed();
            DuplicateProjectMemberFailsClosed();
            DuplicateArrayObjectMemberFailsClosed();
            UnknownMemberContractRemainsStable();
            UniqueControlStillReachesSemanticValidation();
        }

        private static void DuplicateRootMemberFailsClosed()
        {
            var json = ValidEmptySnapshot().Replace(
                "\"formatVersion\":1,",
                "\"formatVersion\":1,\"formatVersion\":1,");
            RequireDuplicate(ProjectInterchangeJsonValidator.Validate(json), "$");
        }

        private static void DuplicateProjectMemberFailsClosed()
        {
            var json = ValidEmptySnapshot().Replace(
                "\"schemaVersion\":1,",
                "\"schemaVersion\":1,\"schemaVersion\":1,");
            RequireDuplicate(ProjectInterchangeJsonValidator.Validate(json), "$.project");
        }

        private static void DuplicateArrayObjectMemberFailsClosed()
        {
            var json = ValidEmptySnapshot().Replace(
                "\"zones\":[]",
                "\"zones\":[{\"id\":\"Z-1\",\"id\":\"Z-1\",\"name\":\"Zone 1\"}]");
            RequireDuplicate(ProjectInterchangeJsonValidator.Validate(json), "$.zones[0]");
        }

        private static void UnknownMemberContractRemainsStable()
        {
            var json = ValidEmptySnapshot().Replace(
                "\"formatVersion\":1,",
                "\"formatVersion\":1,\"unsupported\":true,");
            RequireError(ProjectInterchangeJsonValidator.Validate(json), "JSON_UNKNOWN_MEMBER");
        }

        private static void UniqueControlStillReachesSemanticValidation()
        {
            var result = ProjectInterchangeJsonValidator.Validate(ValidEmptySnapshot());
            if (!result.IsValid)
                throw new InvalidOperationException(
                    "Unique interchange control must remain valid; got " +
                    string.Join(",", result.Issues.Select(x => x.Code)) + ".");
        }

        private static string ValidEmptySnapshot() =>
            "{\"format\":\"QS3D.SemanticSnapshot\",\"formatVersion\":1," +
            "\"units\":{\"length\":\"m\",\"area\":\"m2\",\"volume\":\"m3\",\"mass\":\"kg\"}," +
            "\"project\":{\"id\":\"P\",\"name\":\"N\",\"schemaVersion\":1,\"drawingFingerprint\":\"\",\"updatedUtc\":\"2026-08-10T11:00:00.0000000Z\"}," +
            "\"zones\":[],\"floors\":[],\"families\":[],\"elements\":[]}";

        private static void RequireDuplicate(ProjectInterchangeValidationResult result, string expectedPath)
        {
            var issue = result.Issues.FirstOrDefault(x =>
                x.Severity == InterchangeValidationSeverity.Error &&
                string.Equals(x.Code, "JSON_DUPLICATE_MEMBER", StringComparison.Ordinal));
            if (issue == null)
                throw new InvalidOperationException(
                    "Expected JSON_DUPLICATE_MEMBER; got " +
                    string.Join(",", result.Issues.Select(x => x.Code)) + ".");
            if (!string.Equals(issue.Path, expectedPath, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Duplicate-member path changed. Expected=" + expectedPath + ", actual=" + issue.Path + ".");
        }

        private static void RequireError(ProjectInterchangeValidationResult result, string code)
        {
            if (!result.Issues.Any(x =>
                x.Severity == InterchangeValidationSeverity.Error &&
                string.Equals(x.Code, code, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    "Expected " + code + "; got " +
                    string.Join(",", result.Issues.Select(x => x.Code)) + ".");
            }
        }
    }
}
