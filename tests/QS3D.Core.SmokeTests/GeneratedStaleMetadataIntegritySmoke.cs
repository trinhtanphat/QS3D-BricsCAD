using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedStaleMetadataIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var project = new ProjectState("P-STALE-META", "Generated stale metadata smoke");
            var element = new ProjectElement("E1", ElementCategory.Beam);
            project.Elements.Add(element);

            element.Properties[ProjectElement.GeneratedSolidStateKey] = "stale";
            var missingSnapshot = new GeneratedGeometryStaleHealthService().Inspect(project);
            HasCode(missingSnapshot, "GENERATED_STALE_METADATA_INVALID", "missing stale snapshot");

            element.Properties["GeneratedSolidHandle"] = "NEW";
            element.Properties[ProjectElement.GeneratedSolidStaleSnapshotKey] = "OLD";
            var mismatchedSnapshot = new GeneratedGeometryStaleHealthService().Inspect(project);
            LacksCode(mismatchedSnapshot, "GENERATED_STALE_METADATA_INVALID", "nonblank mismatched snapshot is not corrupt metadata");
            LacksCode(mismatchedSnapshot, "GENERATED_SOLID_STALE", "old stale mark must not carry to rebuilt output");

            element.Properties[ProjectElement.GeneratedSolidStaleSnapshotKey] = "NEW";
            var matchingSnapshot = new GeneratedGeometryStaleHealthService().Inspect(project);
            LacksCode(matchingSnapshot, "GENERATED_STALE_METADATA_INVALID", "matching snapshot metadata integrity");
            HasCode(matchingSnapshot, "GENERATED_SOLID_STALE", "matching stale snapshot warning");
        }

        private static void HasCode(System.Collections.Generic.IEnumerable<ModelHealthIssue> issues, string code, string label)
        {
            if (issues.Any(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase))) return;
            throw new Exception("GeneratedStaleMetadataIntegritySmoke missing " + code + ": " + label + ".");
        }

        private static void LacksCode(System.Collections.Generic.IEnumerable<ModelHealthIssue> issues, string code, string label)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase))) return;
            throw new Exception("GeneratedStaleMetadataIntegritySmoke unexpectedly found " + code + ": " + label + ".");
        }
    }
}
