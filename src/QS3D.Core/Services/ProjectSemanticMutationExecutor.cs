using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.Services
{
    public enum ProjectSemanticMutationPhase
    {
        Planned = 0,
        Running = 1,
        Validating = 2,
        Committed = 3,
        RollingBack = 4,
        RolledBack = 5,
        RollbackFailed = 6
    }

    public sealed class ProjectSemanticMutationJournalEntry
    {
        internal ProjectSemanticMutationJournalEntry(int sequence, string operationName, ProjectSemanticMutationPhase phase, string detail)
        {
            Sequence = sequence;
            OperationName = operationName ?? string.Empty;
            Phase = phase;
            Detail = detail ?? string.Empty;
        }

        public int Sequence { get; }
        public string OperationName { get; }
        public ProjectSemanticMutationPhase Phase { get; }
        public string Detail { get; }
    }

    public sealed class ProjectSemanticMutationJournal
    {
        private const int MaxEntries = 256;
        private readonly List<ProjectSemanticMutationJournalEntry> _entries = new List<ProjectSemanticMutationJournalEntry>();

        public IReadOnlyList<ProjectSemanticMutationJournalEntry> Entries =>
            new ReadOnlyCollection<ProjectSemanticMutationJournalEntry>(new List<ProjectSemanticMutationJournalEntry>(_entries));

        internal void Record(string operationName, ProjectSemanticMutationPhase phase, string detail)
        {
            if (_entries.Count >= MaxEntries)
                throw new InvalidOperationException("Project semantic mutation journal exceeds the supported " + MaxEntries + " entry limit.");
            _entries.Add(new ProjectSemanticMutationJournalEntry(_entries.Count + 1, operationName, phase, detail));
        }
    }

    /// <summary>
    /// Provides one rollback-protected ProjectState mutation boundary with a detached phase journal.
    /// This executor restores semantic project state only; it does not roll back BricsCAD native transactions.
    /// </summary>
    public static class ProjectSemanticMutationExecutor
    {
        private const int MaxOperationNameLength = 160;
        private const int MaxDetailLength = 1000;

        public static T Execute<T>(
            ProjectState project,
            string operationName,
            Func<T> mutation,
            ProjectSemanticMutationJournal? journal = null)
        {
            return Execute(project, operationName, mutation, null, journal);
        }

        public static T Execute<T>(
            ProjectState project,
            string operationName,
            Func<T> mutation,
            Action? preCommitValidation,
            ProjectSemanticMutationJournal? journal = null)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (mutation == null) throw new ArgumentNullException(nameof(mutation));
            var operation = NormalizeOperationName(operationName);
            var effectiveJournal = journal ?? new ProjectSemanticMutationJournal();
            var rollback = ProjectStateSnapshot.Capture(project);

            TryRecord(effectiveJournal, operation, ProjectSemanticMutationPhase.Planned, "ProjectState snapshot captured.");
            try
            {
                TryRecord(effectiveJournal, operation, ProjectSemanticMutationPhase.Running, "Semantic mutation started.");
                var result = mutation();
                if (preCommitValidation != null)
                {
                    TryRecord(effectiveJournal, operation, ProjectSemanticMutationPhase.Validating, "Pre-commit validation started.");
                    preCommitValidation();
                }
                TryRecord(effectiveJournal, operation, ProjectSemanticMutationPhase.Committed, "Semantic mutation committed.");
                return result;
            }
            catch (Exception operationError)
            {
                TryRecord(effectiveJournal, operation, ProjectSemanticMutationPhase.RollingBack, SafeDetail(operationError));
                try
                {
                    rollback.Restore(project);
                    TryRecord(effectiveJournal, operation, ProjectSemanticMutationPhase.RolledBack, "ProjectState restored to the captured snapshot.");
                }
                catch (Exception rollbackError)
                {
                    TryRecord(effectiveJournal, operation, ProjectSemanticMutationPhase.RollbackFailed, SafeDetail(rollbackError));
                    throw new InvalidOperationException(
                        "Project semantic mutation failed and ProjectState rollback also failed for operation " + operation + ".",
                        new AggregateException(operationError, rollbackError));
                }
                throw;
            }
        }

        private static string NormalizeOperationName(string value)
        {
            var supplied = value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(supplied))
                throw new ArgumentException("Project semantic mutation operation name is required.", nameof(value));
            var normalized = supplied.Trim();
            if (!string.Equals(supplied, normalized, StringComparison.Ordinal))
                throw new ArgumentException("Project semantic mutation operation name must not contain leading or trailing whitespace.", nameof(value));
            if (normalized.Length > MaxOperationNameLength)
                throw new ArgumentException("Project semantic mutation operation name exceeds the supported length.", nameof(value));
            foreach (var character in normalized)
                if (char.IsControl(character))
                    throw new ArgumentException("Project semantic mutation operation name contains control characters.", nameof(value));
            return normalized;
        }

        private static string SafeDetail(Exception error)
        {
            var detail = (error == null ? "Exception" : error.GetType().Name) + " occurred.";
            if (detail.Length > MaxDetailLength) detail = detail.Substring(0, MaxDetailLength);
            return detail;
        }

        private static void TryRecord(
            ProjectSemanticMutationJournal journal,
            string operation,
            ProjectSemanticMutationPhase phase,
            string detail)
        {
            try { journal.Record(operation, phase, detail); }
            catch { }
        }
    }
}
