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

            if (!stamp.IsForPath(path))
                throw new InvalidOperationException("Revision stamp must recognize its exact primary path.");
            if (!stamp.Equals(sameStamp) || stamp.GetHashCode() != sameStamp.GetHashCode())
                throw new InvalidOperationException("Equivalent sidecar revision stamps must remain equal with matching hash codes.");

            var whitespaceFileName = " QS3D-SIDECAR-" + Guid.NewGuid().ToString("N") + "-SPACE.qsdb";
            var whitespacePath = Path.Combine(Path.GetTempPath(), whitespaceFileName);
            var trimmedNeighborPath = Path.Combine(Path.GetTempPath(), whitespaceFileName.TrimStart());
            if (string.Equals(whitespacePath, trimmedNeighborPath, StringComparison.Ordinal))
                throw new InvalidOperationException("Whitespace path smoke fixture must have a distinct trimmed neighbor.");

            var whitespaceStamp = ProjectSidecarRevisionStamp.Capture(whitespacePath);
            var trimmedNeighborStamp = ProjectSidecarRevisionStamp.Capture(trimmedNeighborPath);
            if (whitespaceStamp.HasAnyFile || trimmedNeighborStamp.HasAnyFile)
                throw new InvalidOperationException("Whitespace path smoke must use non-existing primary/backup paths.");
            if (!whitespaceStamp.IsForPath(whitespacePath))
                throw new InvalidOperationException("Revision stamp must preserve a valid leading-whitespace file name.");
            if (whitespaceStamp.IsForPath(trimmedNeighborPath) || whitespaceStamp.Equals(trimmedNeighborStamp))
                throw new InvalidOperationException("Revision stamp must not collapse a valid whitespace path onto its trimmed neighbor.");

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
