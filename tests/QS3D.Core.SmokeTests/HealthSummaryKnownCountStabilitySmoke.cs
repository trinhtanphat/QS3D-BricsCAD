using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;

namespace QS3D.Core.SmokeTests
{
    internal static class HealthSummaryKnownCountStabilitySmoke
    {
        internal static void Run()
        {
            KnownCountOverrunFailsBeforeThrowingTail();
            GenericCountDriftFailsClosed();
            ReadOnlyCountDriftFailsClosed();
            NonGenericCountDriftFailsClosed();
            PostTraversalNegativeCountFailsClosed();
            KnownCountUnderYieldStillFailsClosed();
            StableCountedAndStreamingInputsRemainSupported();
        }

        private static void KnownCountOverrunFailsBeforeThrowingTail()
        {
            var source = new OverrunThenThrowCollection();
            ThrowsContaining<InvalidOperationException>(
                () => new HealthSummary(source),
                "more diagnostic issues than its known count");
            Equal(2, source.ObservedEntries);
        }

        private static void GenericCountDriftFailsClosed()
        {
            ThrowsContaining<InvalidOperationException>(
                () => new HealthSummary(new GenericCountDriftCollection(Issues(2), 2, 1)),
                "known issue count changed during traversal");
        }

        private static void ReadOnlyCountDriftFailsClosed()
        {
            ThrowsContaining<InvalidOperationException>(
                () => new HealthSummary(new ReadOnlyCountDriftCollection(Issues(2), 2, 3)),
                "known issue count changed during traversal");
        }

        private static void NonGenericCountDriftFailsClosed()
        {
            ThrowsContaining<InvalidOperationException>(
                () => new HealthSummary(new NonGenericCountDriftCollection(Issues(2), 2, 0)),
                "known issue count changed during traversal");
        }

        private static void PostTraversalNegativeCountFailsClosed()
        {
            ThrowsContaining<InvalidOperationException>(
                () => new HealthSummary(new GenericCountDriftCollection(Issues(2), 2, -1)),
                "invalid negative known issue count");
        }

        private static void KnownCountUnderYieldStillFailsClosed()
        {
            ThrowsContaining<InvalidOperationException>(
                () => new HealthSummary(new GenericCountDriftCollection(Issues(1), 2, 2)),
                "known issue count does not match enumerated issue count");
        }

        private static void StableCountedAndStreamingInputsRemainSupported()
        {
            var counted = new List<ModelHealthIssue>
            {
                Issue("E", HealthSeverity.Error),
                Issue("W", HealthSeverity.Warning),
                Issue("I", HealthSeverity.Info)
            };
            var summary = new HealthSummary(counted);
            Equal(3, summary.Issues.Count);
            Equal(1, summary.Errors);
            Equal(1, summary.Warnings);
            Equal(1, summary.Info);
            Equal(false, summary.IsHealthy);
            Equal(false, summary.IsReleaseReady);
            SequenceEqual(new[] { "E", "W", "I" }, summary.Issues.Select(x => x.Code));

            var streamed = new HealthSummary(Streaming(
                Issue("A", HealthSeverity.Info),
                Issue("B", HealthSeverity.Info)));
            Equal(2, streamed.Issues.Count);
            Equal(0, streamed.Errors);
            Equal(0, streamed.Warnings);
            Equal(2, streamed.Info);
            Equal(true, streamed.IsHealthy);
            Equal(true, streamed.IsReleaseReady);
        }

        private static ModelHealthIssue[] Issues(int count)
        {
            return Enumerable.Range(0, count)
                .Select(i => Issue("I" + i, HealthSeverity.Info))
                .ToArray();
        }

        private static ModelHealthIssue Issue(string code, HealthSeverity severity)
            => new ModelHealthIssue(code, severity, code + " message");

        private static IEnumerable<ModelHealthIssue> Streaming(params ModelHealthIssue[] values)
        {
            foreach (var value in values) yield return value;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
        }

        private static void SequenceEqual(IEnumerable<string> expected, IEnumerable<string> actual)
        {
            if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
                throw new Exception("Expected [" + string.Join(", ", expected) + "] but got [" + string.Join(", ", actual) + "].");
        }

        private static void ThrowsContaining<T>(Action action, string expectedText) where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                if (ex.Message.IndexOf(expectedText, StringComparison.Ordinal) >= 0) return;
                throw new Exception("Expected exception message containing '" + expectedText + "', got '" + ex.Message + "'.");
            }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private sealed class OverrunThenThrowCollection : ICollection<ModelHealthIssue>
        {
            public int ObservedEntries { get; private set; }
            public int Count => 1;
            public bool IsReadOnly => true;

            public IEnumerator<ModelHealthIssue> GetEnumerator()
            {
                ObservedEntries++;
                yield return Issue("A", HealthSeverity.Info);
                ObservedEntries++;
                yield return Issue("B", HealthSeverity.Info);
                throw new Exception("HealthSummary advanced beyond the known-count overrun boundary.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(ModelHealthIssue item) => false;
            public void CopyTo(ModelHealthIssue[] array, int arrayIndex) => throw new NotSupportedException();
            public void Add(ModelHealthIssue item) => throw new NotSupportedException();
            public bool Remove(ModelHealthIssue item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
        }

        private sealed class GenericCountDriftCollection : ICollection<ModelHealthIssue>
        {
            private readonly ModelHealthIssue[] _values;
            private readonly int _before;
            private readonly int _after;
            private bool _completed;

            public GenericCountDriftCollection(ModelHealthIssue[] values, int before, int after)
            {
                _values = values;
                _before = before;
                _after = after;
            }

            public int Count => _completed ? _after : _before;
            public bool IsReadOnly => true;

            public IEnumerator<ModelHealthIssue> GetEnumerator()
            {
                for (var i = 0; i < _values.Length; i++) yield return _values[i];
                _completed = true;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(ModelHealthIssue item) => false;
            public void CopyTo(ModelHealthIssue[] array, int arrayIndex) => throw new NotSupportedException();
            public void Add(ModelHealthIssue item) => throw new NotSupportedException();
            public bool Remove(ModelHealthIssue item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
        }

        private sealed class ReadOnlyCountDriftCollection : IReadOnlyCollection<ModelHealthIssue>
        {
            private readonly ModelHealthIssue[] _values;
            private readonly int _before;
            private readonly int _after;
            private bool _completed;

            public ReadOnlyCountDriftCollection(ModelHealthIssue[] values, int before, int after)
            {
                _values = values;
                _before = before;
                _after = after;
            }

            public int Count => _completed ? _after : _before;

            public IEnumerator<ModelHealthIssue> GetEnumerator()
            {
                for (var i = 0; i < _values.Length; i++) yield return _values[i];
                _completed = true;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class NonGenericCountDriftCollection : IEnumerable<ModelHealthIssue>, ICollection
        {
            private readonly ModelHealthIssue[] _values;
            private readonly int _before;
            private readonly int _after;
            private bool _completed;

            public NonGenericCountDriftCollection(ModelHealthIssue[] values, int before, int after)
            {
                _values = values;
                _before = before;
                _after = after;
            }

            public int Count => _completed ? _after : _before;
            public bool IsSynchronized => false;
            public object SyncRoot => this;

            public IEnumerator<ModelHealthIssue> GetEnumerator()
            {
                for (var i = 0; i < _values.Length; i++) yield return _values[i];
                _completed = true;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }
    }

    internal static class HealthSummaryKnownCountStabilitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => HealthSummaryKnownCountStabilitySmoke.Run();
    }
}
