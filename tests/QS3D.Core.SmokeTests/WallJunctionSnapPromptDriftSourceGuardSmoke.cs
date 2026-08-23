using System;
using System.IO;

namespace QS3D.Core.SmokeTests
{
    internal static class WallJunctionSnapPromptDriftSourceGuardSmoke
    {
        internal static void Run()
        {
            var root = FindRepositoryRoot();
            var snapPath = Path.Combine(root, "src", "QS3D.BricsCAD.V25", "WallJunctionSnapCommands.cs");
            var mutationPath = Path.Combine(root, "src", "QS3D.BricsCAD.V25", "ExistingProjectMutationContext.cs");
            var coordinatorPath = Path.Combine(root, "src", "QS3D.BricsCAD.V25", "ProjectContextCoordinator.cs");
            var source = File.ReadAllText(snapPath);
            var mutationSource = File.ReadAllText(mutationPath);
            var coordinatorSource = File.ReadAllText(coordinatorPath);

            const string signature = "private static ProjectState RequireFreshMutationProject";
            var start = source.IndexOf(signature, StringComparison.Ordinal);
            Require(start >= 0, "Wall Snap fresh mutation helper was not found.");
            var end = source.IndexOf("private static void RequireTouchHeadroom", start, StringComparison.Ordinal);
            Require(end > start, "Wall Snap fresh mutation helper boundary was not found.");
            var helper = source.Substring(start, end - start);

            var projectMismatch = helper.IndexOf("!string.Equals(project.ProjectId, expectedProjectId", StringComparison.Ordinal);
            var versionMismatch = helper.IndexOf("project.ChangeVersion != expectedChangeVersion", StringComparison.Ordinal);
            var forget = helper.IndexOf("ProjectContextCoordinator.Forget(document);", StringComparison.Ordinal);
            var refusal = helper.IndexOf("throw new InvalidOperationException", StringComparison.Ordinal);

            Require(projectMismatch >= 0 && versionMismatch >= 0,
                "Wall Snap must continue comparing both ProjectId and ChangeVersion after interactive selection.");
            Require(forget > versionMismatch,
                "Wall Snap prompt-drift refusal must forget the canonical cache after detecting the final bind mismatch.");
            Require(refusal > forget,
                "Wall Snap must forget a newly bound replacement project before returning the prompt-drift refusal.");
            Require(helper.Contains("ExistingProjectMutationContext.Require(document, operation)", StringComparison.Ordinal),
                "Wall Snap must keep the existing canonical mutation binding boundary.");

            Require(mutationSource.Contains("ProjectContextCoordinator.GetOrCreate(document)", StringComparison.Ordinal),
                "ExistingProjectMutationContext canonical bind behavior must remain intact.");
            Require(mutationSource.Contains("ProjectContextCoordinator.RequireBackingStoreUnchanged", StringComparison.Ordinal),
                "ExistingProjectMutationContext backing-store freshness guard must remain intact.");

            var tryCachedStart = coordinatorSource.IndexOf("public static bool TryGetCached(Document document, out ProjectState project)", StringComparison.Ordinal);
            var tryCachedEnd = coordinatorSource.IndexOf("public static void RequireBackingStoreUnchanged", tryCachedStart, StringComparison.Ordinal);
            Require(tryCachedStart >= 0 && tryCachedEnd > tryCachedStart,
                "ProjectContextCoordinator.TryGetCached boundary was not found.");
            var tryCached = coordinatorSource.Substring(tryCachedStart, tryCachedEnd - tryCachedStart);
            Require(tryCached.Contains("Projects.TryGetValue(document, out project)", StringComparison.Ordinal),
                "TryGetCached must continue reporting only entries present in the canonical project cache.");
            Require(tryCached.Contains("return false;", StringComparison.Ordinal),
                "TryGetCached must return false when the document is absent from the canonical project cache.");

            var forgetStart = coordinatorSource.IndexOf("public static void Forget(Document document)", StringComparison.Ordinal);
            var forgetEnd = coordinatorSource.IndexOf("public static void ForgetByName", forgetStart, StringComparison.Ordinal);
            Require(forgetStart >= 0 && forgetEnd > forgetStart,
                "ProjectContextCoordinator.Forget boundary was not found.");
            var forgetBody = coordinatorSource.Substring(forgetStart, forgetEnd - forgetStart);
            Require(forgetBody.Contains("Projects.Remove(document);", StringComparison.Ordinal),
                "Forget must remove the document from the same canonical cache observed by TryGetCached.");
        }

        private static string FindRepositoryRoot()
        {
            for (var current = new DirectoryInfo(AppContext.BaseDirectory); current != null; current = current.Parent)
            {
                if (File.Exists(Path.Combine(current.FullName, "QS3D.sln")) &&
                    Directory.Exists(Path.Combine(current.FullName, "src")))
                    return current.FullName;
            }

            throw new InvalidOperationException("Could not locate the QS3D repository root for the Wall Snap prompt-drift source guard.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
