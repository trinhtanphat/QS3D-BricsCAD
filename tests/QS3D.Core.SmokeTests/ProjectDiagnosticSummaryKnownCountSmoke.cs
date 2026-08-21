using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectDiagnosticSummaryKnownCountSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var project = new ProjectState("P-DIAG-KNOWN-COUNT", "Diagnostic count smoke");
            var issue = new ModelHealthIssue("COUNT_CONTROL", HealthSeverity.Warning, "control");

            ExpectRejectedBeforeEnumeration(
                project,
                new KnownCountIssues(-1, -1, -1, issue),
                "negative count");

            var oversized = ProjectDiagnosticSummaryExporter.MaxIssueCount + 1;
            ExpectRejectedBeforeEnumeration(
                project,
                new KnownCountIssues(oversized, oversized, oversized, issue),
                "at most");

            ExpectRejectedBeforeEnumeration(
                project,
                new KnownCountIssues(1, 2, 1, issue),
                "conflicting known counts");

            var honest = new KnownCountIssues(1, 1, 1, issue, allowEnumeration: true);
            var json = ProjectDiagnosticSummaryExporter.Build(project, honest);
            if (honest.EnumerationAttempts != 1)
                throw new InvalidOperationException("Honest diagnostic issue input should be enumerated exactly once.");
            if (json.IndexOf("\"warnings\":1", StringComparison.Ordinal) < 0 ||
                json.IndexOf("\"code\":\"COUNT_CONTROL\"", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Honest diagnostic issue input did not preserve summary output.");
        }

        private static void ExpectRejectedBeforeEnumeration(
            ProjectState project,
            KnownCountIssues issues,
            string expectedMessageFragment)
        {
            try
            {
                ProjectDiagnosticSummaryExporter.Build(project, issues);
                throw new InvalidOperationException("Malformed diagnostic known-count input should be rejected.");
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedMessageFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Unexpected diagnostic known-count rejection: " + ex.Message, ex);
            }

            if (issues.EnumerationAttempts != 0)
                throw new InvalidOperationException("Invalid diagnostic known-count input was enumerated before rejection.");
        }

        private sealed class KnownCountIssues :
            ICollection<ModelHealthIssue>,
            IReadOnlyCollection<ModelHealthIssue>,
            ICollection
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly ModelHealthIssue _issue;
            private readonly bool _allowEnumeration;

            public KnownCountIssues(
                int genericCount,
                int readOnlyCount,
                int nonGenericCount,
                ModelHealthIssue issue,
                bool allowEnumeration = false)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _issue = issue;
                _allowEnumeration = allowEnumeration;
            }

            public int EnumerationAttempts { get; private set; }
            int ICollection<ModelHealthIssue>.Count => _genericCount;
            int IReadOnlyCollection<ModelHealthIssue>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<ModelHealthIssue>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<ModelHealthIssue> GetEnumerator()
            {
                EnumerationAttempts++;
                if (!_allowEnumeration)
                    throw new InvalidOperationException("Enumeration must not start for invalid known-count input.");
                yield return _issue;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection<ModelHealthIssue>.Add(ModelHealthIssue item) => throw new NotSupportedException();
            void ICollection<ModelHealthIssue>.Clear() => throw new NotSupportedException();
            bool ICollection<ModelHealthIssue>.Contains(ModelHealthIssue item) => ReferenceEquals(item, _issue);
            void ICollection<ModelHealthIssue>.CopyTo(ModelHealthIssue[] array, int arrayIndex) => array[arrayIndex] = _issue;
            bool ICollection<ModelHealthIssue>.Remove(ModelHealthIssue item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => array.SetValue(_issue, index);
        }
    }
}
