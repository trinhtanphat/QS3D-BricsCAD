using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectSemanticMutationJournalRedactionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            const string privateMarker = "PRIVATE_PATH=/users/example/private/project.qsdb";
            var project = new ProjectState("P-JOURNAL-REDACT", "Journal redaction smoke");
            var journal = new ProjectSemanticMutationJournal();

            Throws<InvalidOperationException>(() => ProjectSemanticMutationExecutor.Execute<int>(
                project,
                "journal-redaction",
                () =>
                {
                    project.Metadata["Transient"] = "yes";
                    throw new InvalidOperationException(privateMarker);
                },
                journal));

            if (project.Metadata.ContainsKey("Transient"))
                throw new Exception("ProjectSemanticMutationJournalRedactionSmoke rollback did not restore project metadata.");
            if (!journal.Entries.Any(x => x.Phase == ProjectSemanticMutationPhase.RollingBack && x.Detail.Contains("InvalidOperationException", StringComparison.Ordinal)))
                throw new Exception("ProjectSemanticMutationJournalRedactionSmoke did not retain exception type evidence.");
            if (!journal.Entries.Any(x => x.Phase == ProjectSemanticMutationPhase.RolledBack))
                throw new Exception("ProjectSemanticMutationJournalRedactionSmoke did not retain rollback completion evidence.");
            if (journal.Entries.Any(x => x.Detail.Contains(privateMarker, StringComparison.Ordinal)))
                throw new Exception("ProjectSemanticMutationJournalRedactionSmoke leaked raw exception detail into the journal.");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException ex)
            {
                if (!string.Equals(ex.Message, "PRIVATE_PATH=/users/example/private/project.qsdb", StringComparison.Ordinal))
                    throw new Exception("ProjectSemanticMutationJournalRedactionSmoke changed the exception exposed to the caller.");
                return;
            }
            throw new Exception("ProjectSemanticMutationJournalRedactionSmoke expected " + typeof(TException).Name + ".");
        }
    }
}
