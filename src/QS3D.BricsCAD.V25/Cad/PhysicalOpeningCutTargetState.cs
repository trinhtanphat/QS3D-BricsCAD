using System.Collections.Generic;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class PhysicalOpeningCutTargetState
    {
        public const string OpeningIdsKey = PhysicalOpeningCutTargetStateCodec.OpeningIdsKey;

        public static bool TryRead(ProjectElement host, out IReadOnlyList<string> openingIds) =>
            PhysicalOpeningCutTargetStateCodec.TryRead(host, out openingIds);

        public static IReadOnlyList<ProjectElement> Resolve(ProjectState project, ProjectElement host, IEnumerable<string> openingIds) =>
            PhysicalOpeningCutTargetStateCodec.Resolve(project, host, openingIds);

        public static void Write(ProjectElement host, IEnumerable<string> openingIds) =>
            PhysicalOpeningCutTargetStateCodec.Write(host, openingIds);

        public static IReadOnlyList<string> Normalize(IEnumerable<string> openingIds) =>
            PhysicalOpeningCutTargetStateCodec.Normalize(openingIds);
    }
}
