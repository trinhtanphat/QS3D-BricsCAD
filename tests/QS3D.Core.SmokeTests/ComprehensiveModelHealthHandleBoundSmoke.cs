using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ComprehensiveModelHealthHandleBoundSmoke
    {
        private const int MaximumHandles = 10000;

        internal static void Run()
        {
            CountedSourceOversizeFailsBeforeEnumerationAndMutation();
            CountedGeneratedOversizeFailsBeforeEnumerationAndMutation();
            StreamingSourceOversizeStopsAtFirstDisallowedEntry();
            StreamingGeneratedOversizeStopsAtFirstDisallowedEntry();
            ExactBoundaryAndNullInputsRemainAccepted();
        }

        private static void CountedSourceOversizeFailsBeforeEnumerationAndMutation()
        {
            var project = Project();
            var source = new ProbeSet(MaximumHandles + 1, MaximumHandles + 2);
            var beforeVersion = project.ChangeVersion;

            var error = Capture<InvalidOperationException>(() =>
                new ComprehensiveModelHealthService(1).Inspect(project, source, null));

            Equal(0, source.GetEnumeratorCalls, "Known oversized live-source input must fail before enumeration.");
            Equal(beforeVersion, project.ChangeVersion, "Rejected live-source input must not mutate project state.");
            Contains("live source Handle input", error.Message, "Source oversize diagnostic must identify the input boundary.");
            Contains("10000", error.Message, "Source oversize diagnostic must report the supported bound.");
        }

        private static void CountedGeneratedOversizeFailsBeforeEnumerationAndMutation()
        {
            var project = Project();
            var source = new ProbeSet(MaximumHandles + 1, MaximumHandles + 2);
            var beforeVersion = project.ChangeVersion;

            var error = Capture<InvalidOperationException>(() =>
                new ComprehensiveModelHealthService(1).Inspect(project, null, source));

            Equal(0, source.GetEnumeratorCalls, "Known oversized generated-handle input must fail before enumeration.");
            Equal(beforeVersion, project.ChangeVersion, "Rejected generated-handle input must not mutate project state.");
            Contains("live generated-solid Handle input", error.Message, "Generated oversize diagnostic must identify the input boundary.");
            Contains("10000", error.Message, "Generated oversize diagnostic must report the supported bound.");
        }

        private static void StreamingSourceOversizeStopsAtFirstDisallowedEntry()
        {
            var project = Project();
            var source = new ProbeSet(1, MaximumHandles + 2);
            var beforeVersion = project.ChangeVersion;

            Capture<InvalidOperationException>(() =>
                new ComprehensiveModelHealthService(1).Inspect(project, source, null));

            Equal(MaximumHandles + 1, source.YieldedCount, "Streaming live-source ingest must stop on entry 10,001.");
            Equal(beforeVersion, project.ChangeVersion, "Streaming source rejection must happen before project mutation.");
        }

        private static void StreamingGeneratedOversizeStopsAtFirstDisallowedEntry()
        {
            var project = Project();
            var source = new ProbeSet(1, MaximumHandles + 2);
            var beforeVersion = project.ChangeVersion;

            Capture<InvalidOperationException>(() =>
                new ComprehensiveModelHealthService(1).Inspect(project, null, source));

            Equal(MaximumHandles + 1, source.YieldedCount, "Streaming generated-handle ingest must stop on entry 10,001.");
            Equal(beforeVersion, project.ChangeVersion, "Streaming generated rejection must happen before project mutation.");
        }

        private static void ExactBoundaryAndNullInputsRemainAccepted()
        {
            var project = Project();
            var source = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < MaximumHandles; index++)
                source.Add("  S" + index.ToString("D5", System.Globalization.CultureInfo.InvariantCulture) + "  ");

            new ComprehensiveModelHealthService(1).Inspect(project, source, null);
            new ComprehensiveModelHealthService(1).Inspect(project, null, null);

            Equal(0L, project.ChangeVersion, "Read-only comprehensive diagnostics must not mutate an empty project.");
        }

        private static ProjectState Project()
        {
            return new ProjectState("health-handle-bound", "Health Handle Bound");
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class ProbeSet : ISet<string>
        {
            private readonly int _reportedCount;
            private readonly int _yieldCount;

            internal ProbeSet(int reportedCount, int yieldCount)
            {
                _reportedCount = reportedCount;
                _yieldCount = yieldCount;
            }

            public int Count => _reportedCount;
            public bool IsReadOnly => true;
            internal int GetEnumeratorCalls { get; private set; }
            internal int YieldedCount { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                GetEnumeratorCalls++;
                for (var index = 0; index < _yieldCount; index++)
                {
                    YieldedCount++;
                    yield return "H" + index.ToString("D5", System.Globalization.CultureInfo.InvariantCulture);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public bool Contains(string item) => false;
            public void CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();
            public bool IsProperSubsetOf(IEnumerable<string> other) => throw new NotSupportedException();
            public bool IsProperSupersetOf(IEnumerable<string> other) => throw new NotSupportedException();
            public bool IsSubsetOf(IEnumerable<string> other) => throw new NotSupportedException();
            public bool IsSupersetOf(IEnumerable<string> other) => throw new NotSupportedException();
            public bool Overlaps(IEnumerable<string> other) => throw new NotSupportedException();
            public bool SetEquals(IEnumerable<string> other) => throw new NotSupportedException();
            public void ExceptWith(IEnumerable<string> other) => throw new NotSupportedException();
            public void IntersectWith(IEnumerable<string> other) => throw new NotSupportedException();
            public void SymmetricExceptWith(IEnumerable<string> other) => throw new NotSupportedException();
            public void UnionWith(IEnumerable<string> other) => throw new NotSupportedException();
            public bool Add(string item) => throw new NotSupportedException();
            void ICollection<string>.Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
        }
    }

    internal static class ComprehensiveModelHealthHandleBoundRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ComprehensiveModelHealthHandleBoundSmoke.Run();
        }
    }
}
