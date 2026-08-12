using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectSidecarRevisionPathSemanticsSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var fileName = "QS3D-SIDECAR-" + Guid.NewGuid().ToString("N") + "-CASE.qsdb";
            var path = Path.Combine(Path.GetTempPath(), fileName);
            var caseVariantPath = Path.Combine(Path.GetTempPath(), fileName.ToLowerInvariant());
            if (string.Equals(path, caseVariantPath, StringComparison.Ordinal))
                throw new InvalidOperationException("Sidecar path smoke fixture must differ by casing.");

            var stamp = ProjectSidecarRevisionStamp.Capture(path);
            var sameStamp = ProjectSidecarRevisionStamp.Capture(path);
            var caseVariantStamp = ProjectSidecarRevisionStamp.Capture(caseVariantPath);
            if (stamp.HasAnyFile || sameStamp.HasAnyFile || caseVariantStamp.HasAnyFile)
                throw new InvalidOperationException("Sidecar path smoke must use non-existing primary/backup paths.");

            if (!stamp.IsForPath("  " + path + "  "))
                throw new InvalidOperationException("Revision stamp must recognize its normalized primary path.");
            if (!stamp.Equals(sameStamp) || stamp.GetHashCode() != sameStamp.GetHashCode())
                throw new InvalidOperationException("Equivalent sidecar revision stamps must remain equal with matching hash codes.");

            var caseInsensitive = Path.DirectorySeparatorChar == '\\';
            if (stamp.IsForPath(caseVariantPath) != caseInsensitive)
                throw new InvalidOperationException("Sidecar IsForPath casing semantics must match the repository platform path policy.");
            if (stamp.Equals(caseVariantStamp) != caseInsensitive)
                throw new InvalidOperationException("Sidecar stamp equality casing semantics must match the repository platform path policy.");
            if (caseInsensitive && stamp.GetHashCode() != caseVariantStamp.GetHashCode())
                throw new InvalidOperationException("Equal case-varied sidecar stamps must have matching hash codes on Windows.");
        }
    }
}
