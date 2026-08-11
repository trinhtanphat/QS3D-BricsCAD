using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ModelHealthRecoveryRedactionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            const string sentinel = "PRIVATE_RECOVERY_SENTINEL";
            const string warning = sentinel + @" C:\Users\Alice\secret\project.qsdb";

            var protectedProject = new ProjectState("recovery-redaction", "Recovery redaction");
            protectedProject.Metadata["QS3D.ReadOnlyRecoveryRequired"] = "true";
            protectedProject.Metadata["QS3D.LoadWarning"] = warning;
            var versionBefore = protectedProject.ChangeVersion;

            var protectedIssues = new ModelHealthService().Inspect(protectedProject);
            var loadFailure = protectedIssues.Single(x => string.Equals(x.Code, "PROJECT_LOAD_FAILED", StringComparison.Ordinal));
            if (loadFailure.Severity != HealthSeverity.Error)
                throw new Exception("Protected project load failure must remain an Error.");
            if (loadFailure.Message.IndexOf(sentinel, StringComparison.OrdinalIgnoreCase) >= 0 ||
                loadFailure.Message.IndexOf(@"C:\Users\Alice", StringComparison.OrdinalIgnoreCase) >= 0 ||
                loadFailure.Message.IndexOf("secret", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new Exception("Model Health must not expose raw recovery warning detail.");
            if (!protectedProject.Metadata.TryGetValue("QS3D.LoadWarning", out var warningAfter) || !string.Equals(warningAfter, warning, StringComparison.Ordinal))
                throw new Exception("Model Health inspection must not mutate recovery warning metadata.");
            if (protectedProject.ChangeVersion != versionBefore)
                throw new Exception("Model Health recovery inspection must remain read-only.");

            var recoveredProject = new ProjectState("recovered-backup", "Recovered backup");
            recoveredProject.Metadata["QS3D.RecoveredFromBackup"] = "true";
            var recoveredIssues = new ModelHealthService().Inspect(recoveredProject);
            var recoveredWarning = recoveredIssues.Single(x => string.Equals(x.Code, "PROJECT_RECOVERED_BACKUP", StringComparison.Ordinal));
            if (recoveredWarning.Severity != HealthSeverity.Warning)
                throw new Exception("Backup recovery diagnostic severity changed unexpectedly.");
        }
    }
}