using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeSnapshotTimestampDiffSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            IdenticalCanonicalTimestampsDoNotCreateFalseChange();
            DifferentCanonicalInstantsStillCreateChange();
        }

        private static void IdenticalCanonicalTimestampsDoNotCreateFalseChange()
        {
            var left = Json();
            var right = Json();
            var diff = ProjectInterchangeSnapshotDiff.CompareJson(left, right);
            if (diff.Changes.Any(x => x.ObjectKind == InterchangeSnapshotObjectKind.Project && x.Fields.Contains("updatedUtc")))
                throw new InvalidOperationException("ProjectInterchangeSnapshotTimestampDiffSmoke: identical canonical UTC timestamps produced a false project timestamp change.");
        }

        private static void DifferentCanonicalInstantsStillCreateChange()
        {
            var left = Json();
            var right = left.Replace("2026-08-10T10:00:00.0000000Z", "2026-08-10T11:00:00.0000000Z");
            var diff = ProjectInterchangeSnapshotDiff.CompareJson(left, right);
            if (!diff.Changes.Any(x => x.ObjectKind == InterchangeSnapshotObjectKind.Project && x.Fields.Contains("updatedUtc")))
                throw new InvalidOperationException("ProjectInterchangeSnapshotTimestampDiffSmoke: different canonical UTC timestamps were treated as equal.");
        }

        private static string Json()
        {
            var project = new ProjectState("P-DIFF-TS", "Timestamp diff")
            {
                UpdatedUtc = new DateTime(2026, 8, 10, 10, 0, 0, DateTimeKind.Utc)
            };
            return ProjectInterchangeJsonExporter.Build(project);
        }
    }
}
