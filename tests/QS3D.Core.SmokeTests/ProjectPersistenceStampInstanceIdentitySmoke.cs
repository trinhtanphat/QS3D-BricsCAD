using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectPersistenceStampInstanceIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var project = new ProjectState("STAMP-ID", "Primary");
            var stamp = new ProjectPersistenceStamp(project);
            if (stamp.RequiresSave(project))
                throw new InvalidOperationException("A newly created persistence stamp must be clean for its owning project instance.");

            project.Touch();
            if (!stamp.RequiresSave(project))
                throw new InvalidOperationException("The owning project must become pending after its change version advances.");
            stamp.MarkSaved(project);
            if (stamp.RequiresSave(project) || stamp.SavedChangeVersion != project.ChangeVersion)
                throw new InvalidOperationException("MarkSaved must advance the owning project's saved revision and clear the pending state.");

            var replacement = new ProjectState("STAMP-ID", "Replacement");
            replacement.Touch();
            if (replacement.ChangeVersion != stamp.SavedChangeVersion)
                throw new InvalidOperationException("Smoke setup requires the replacement project to match the stamped change version.");

            RequireDifferentInstanceRejected(() => stamp.RequiresSave(replacement), "RequiresSave");
            RequireDifferentInstanceRejected(() => stamp.MarkSaved(replacement), "MarkSaved");

            var recovered = new ProjectState("STAMP-RECOVERY", "Recovered");
            var recoveryStamp = new ProjectPersistenceStamp(recovered);
            recovered.Metadata["QS3D.RecoveredFromBackup"] = "true";
            if (!recoveryStamp.RequiresSave(recovered))
                throw new InvalidOperationException("Backup-recovery metadata must continue to force a pending save for the owning project instance.");
        }

        private static void RequireDifferentInstanceRejected(Action action, string operation)
        {
            try
            {
                action();
                throw new InvalidOperationException(operation + " must reject a different ProjectState instance even when ProjectId and ChangeVersion match.");
            }
            catch (InvalidOperationException ex)
            {
                if (!string.Equals(ex.Message, "A persistence stamp cannot be reused for a different QS3D project.", StringComparison.Ordinal))
                    throw new InvalidOperationException(operation + " must preserve the canonical persistence-stamp ownership error.", ex);
            }
        }
    }
}
