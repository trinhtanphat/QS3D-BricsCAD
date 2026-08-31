using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectDiagnosticSummaryKnownCountStabilitySmoke
    {
        internal static void Run()
        {
            CountDriftDuringMoveNextFailsClosed();
            CountDriftDuringCurrentFailsClosed();
            StableKnownCountOverYieldRejectsBeforeUnexpectedCurrent();
            StableKnownCountUnderYieldFailsClosed();
            StableCountedAndStreamingSourcesRemainAccepted();
        }

        private static void CountDriftDuringMoveNextFailsClosed()
        {
            var source = new HostileIssues(1, new ModelHealthIssue("DRIFT_MOVE", HealthSeverity.Warning, "x"))
            {
                DriftOnFirstMoveNext = 2
            };
            var error = Capture<InvalidOperationException>(() => Build(source));
            Contains("Count changed during traversal", error.Message);
            Equal(0, source.CurrentReads, "MoveNext Count drift must fail before Current.");
        }

        private static void CountDriftDuringCurrentFailsClosed()
        {
            var source = new HostileIssues(1, new ModelHealthIssue("DRIFT_CURRENT", HealthSeverity.Warning, "x"))
            {
                DriftOnFirstCurrent = 2
            };
            var error = Capture<InvalidOperationException>(() => Build(source));
            Contains("Count changed during traversal", error.Message);
            Equal(1, source.CurrentReads, "Current Count drift must be detected immediately after the single semantic Current read.");
        }

        private static void StableKnownCountOverYieldRejectsBeforeUnexpectedCurrent()
        {
            var source = new HostileIssues(
                1,
                new ModelHealthIssue("FIRST", HealthSeverity.Info, "x"),
                new ModelHealthIssue("EXCESS", HealthSeverity.Error, "must not be read"));
            var error = Capture<InvalidOperationException>(() => Build(source));
            Contains("more items than its admitted known Count", error.Message);
            Equal(1, source.CurrentReads, "N+1 item must be rejected before its Current is read.");
        }

        private static void StableKnownCountUnderYieldFailsClosed()
        {
            var source = new HostileIssues(2, new ModelHealthIssue("ONLY", HealthSeverity.Warning, "x"));
            var error = Capture<InvalidOperationException>(() => Build(source));
            Contains("fewer items than its admitted known Count", error.Message);
            Equal(1, source.CurrentReads, "Under-yield source should read only its actual item.");
        }

        private static void StableCountedAndStreamingSourcesRemainAccepted()
        {
            var counted = new HostileIssues(
                2,
                new ModelHealthIssue("B", HealthSeverity.Warning, "x"),
                new ModelHealthIssue("A", HealthSeverity.Error, "x"));
            var json = Build(counted);
            Contains("\"errors\":1", json);
            Contains("\"warnings\":1", json);
            Equal(2, counted.CurrentReads, "Stable counted source must retain exactly-once Current reads.");

            var streamingJson = Build(Stream(
                new ModelHealthIssue("STREAM", HealthSeverity.Info, "x")));
            Contains("\"info\":1", streamingJson);
        }

        private static string Build(IEnumerable<ModelHealthIssue> issues)
        {
            return ProjectDiagnosticSummaryExporter.Build(
                new ProjectState("P-DIAG-COUNT-STABILITY", "Diagnostic count stability"),
                issues);
        }

        private static IEnumerable<ModelHealthIssue> Stream(params ModelHealthIssue[] issues)
        {
            foreach (var issue in issues)
                yield return issue;
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException ex) { return ex; }
            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Expected fragment '" + expected + "'. Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class HostileIssues : ICollection<ModelHealthIssue>
        {
            private readonly ModelHealthIssue[] _items;
            private int _reportedCount;

            internal HostileIssues(int reportedCount, params ModelHealthIssue[] items)
            {
                _reportedCount = reportedCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            internal int? DriftOnFirstMoveNext { get; set; }
            internal int? DriftOnFirstCurrent { get; set; }
            internal int CurrentReads { get; private set; }
            public int Count => _reportedCount;
            public bool IsReadOnly => true;

            public IEnumerator<ModelHealthIssue> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<ModelHealthIssue>
            {
                private readonly HostileIssues _owner;
                private int _index = -1;

                internal Enumerator(HostileIssues owner) { _owner = owner; }

                public ModelHealthIssue Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_owner.CurrentReads == 1 && _owner.DriftOnFirstCurrent.HasValue)
                            _owner._reportedCount = _owner.DriftOnFirstCurrent.Value;
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _index++;
                    if (_index == 0 && _owner.DriftOnFirstMoveNext.HasValue)
                        _owner._reportedCount = _owner.DriftOnFirstMoveNext.Value;
                    return _index < _owner._items.Length;
                }

                public void Reset() { _index = -1; }
                public void Dispose() { }
            }

            public void Add(ModelHealthIssue item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(ModelHealthIssue item) => throw new NotSupportedException();
            public void CopyTo(ModelHealthIssue[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(ModelHealthIssue item) => throw new NotSupportedException();
        }
    }

    internal static class ProjectDiagnosticSummaryKnownCountStabilityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectDiagnosticSummaryKnownCountStabilitySmoke.Run();
    }
}
