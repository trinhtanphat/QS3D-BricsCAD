using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ComprehensiveModelHealthMulticoreSmoke
    {
        private const int MaximumLiveHandleInputs = 10000;

        internal static void Run()
        {
            ThrowsArgumentOutOfRange(0);
            ThrowsArgumentOutOfRange(5);
            RejectsInvalidKnownLiveHandleCountsBeforeEnumeration();

            var project = NewProject();
            project.Elements.Add(null!);

            var opening = new ProjectElement("opening-1", ElementCategory.WallOpening, string.Empty, "floor-0", "zone-1");
            opening.SourceHandles.Add("AB12");
            project.Elements.Add(opening);

            var grid = new ProjectElement("grid-1", ElementCategory.Grid, string.Empty, "floor-0", "zone-1");
            grid.Properties["GridLabel"] = " A ";
            grid.Properties["GridSequenceIndex"] = "01";
            grid.Properties["GeneratedGridAnnotationHandles"] = "A;A";
            project.Elements.Add(grid);

            var curtain = new ProjectElement("curtain-1", ElementCategory.GlassWall, string.Empty, "floor-0", "zone-1");
            curtain.Properties["GeneratedCurtainFrameHandles"] = "B;B";
            curtain.Properties["GeneratedCurtainFrameCount"] = "2";
            curtain.Properties["GeneratedCurtainFrameColumns"] = "1";
            curtain.Properties["GeneratedCurtainFrameRows"] = "1";
            curtain.Properties["GeneratedRebarHandles"] = "C;C";
            curtain.Properties["GeneratedRebarCount"] = "2";
            project.Elements.Add(curtain);

            var liveSourceHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var singleWorker = new ComprehensiveModelHealthService(1);
            var multiWorker = new ComprehensiveModelHealthService(4);
            var expected = singleWorker.Inspect(project, liveSourceHandles, null);
            if (expected.Count == 0)
                throw new InvalidOperationException("Single-worker comprehensive health oracle unexpectedly produced no diagnostics.");
            RequireCode(expected, "HEALTH_PROVIDER_FAILED");
            RequireCode(expected, "ORPHAN_HANDLE");

            for (var iteration = 0; iteration < 32; iteration++)
            {
                var actual = multiWorker.Inspect(project, liveSourceHandles, null);
                AssertEquivalent(expected, actual, iteration);
            }
        }

        private static void RejectsInvalidKnownLiveHandleCountsBeforeEnumeration()
        {
            var project = NewProject();
            var service = new ComprehensiveModelHealthService(1);

            var conflictingSource = new MultiCountSet(1, 2, 1, throwOnEnumeration: true);
            ThrowsInvalidCountContract(
                () => service.Inspect(project, conflictingSource, null),
                conflictingSource,
                "conflicting Count contracts");

            var negativeGenerated = new MultiCountSet(0, -1, 0, throwOnEnumeration: true);
            ThrowsInvalidCountContract(
                () => service.Inspect(project, null, negativeGenerated),
                negativeGenerated,
                "negative Count contract");

            var oversizedWins = new MultiCountSet(-1, MaximumLiveHandleInputs + 1, 0, throwOnEnumeration: true);
            ThrowsInvalidCountContract(
                () => service.Inspect(project, oversizedWins, null),
                oversizedWins,
                "exceeds the supported bound");

            var consistent = new MultiCountSet(0, 0, 0, throwOnEnumeration: false);
            _ = service.Inspect(project, consistent, null);
            if (!consistent.EnumeratorRequested)
                throw new InvalidOperationException("Consistent live-handle Count contracts should proceed to bounded enumeration.");
        }

        private static void ThrowsInvalidCountContract(Action action, MultiCountSet source, string expectedMessageFragment)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex) when (
                ex.Message.IndexOf(expectedMessageFragment, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (source.EnumeratorRequested)
                    throw new InvalidOperationException("Invalid live-handle Count contracts must fail before enumeration.");
                return;
            }

            throw new InvalidOperationException(
                "Expected comprehensive health live-handle Count-contract rejection containing: " + expectedMessageFragment + ".");
        }

        private static ProjectState NewProject()
        {
            var project = new ProjectState(Guid.NewGuid().ToString("N"), "Multicore diagnostics");
            project.Zones.Add(new ZoneDefinition("zone-1", "Zone 1"));
            project.Floors.Add(new FloorDefinition("floor-0", "Floor 0", 0d));
            project.ActiveZoneId = "zone-1";
            project.ActiveFloorId = "floor-0";
            return project;
        }

        private static void ThrowsArgumentOutOfRange(int maxDegreeOfParallelism)
        {
            try
            {
                _ = new ComprehensiveModelHealthService(maxDegreeOfParallelism);
            }
            catch (ArgumentOutOfRangeException)
            {
                return;
            }
            throw new InvalidOperationException("Expected bounded comprehensive health parallelism validation failure.");
        }

        private static void RequireCode(IReadOnlyList<ModelHealthIssue> issues, string code)
        {
            foreach (var issue in issues)
                if (string.Equals(issue.Code, code, StringComparison.Ordinal)) return;
            throw new InvalidOperationException("Expected comprehensive health diagnostic code was not produced: " + code + ".");
        }

        private static void AssertEquivalent(
            IReadOnlyList<ModelHealthIssue> expected,
            IReadOnlyList<ModelHealthIssue> actual,
            int iteration)
        {
            if (expected.Count != actual.Count)
                throw new InvalidOperationException(
                    "Comprehensive health multicore parity count mismatch at iteration " + iteration +
                    ": expected " + expected.Count + ", actual " + actual.Count + ".");

            for (var index = 0; index < expected.Count; index++)
            {
                var left = expected[index];
                var right = actual[index];
                if (string.Equals(left.Code, right.Code, StringComparison.Ordinal) &&
                    left.Severity == right.Severity &&
                    string.Equals(left.Message, right.Message, StringComparison.Ordinal) &&
                    string.Equals(left.ElementId, right.ElementId, StringComparison.Ordinal))
                    continue;

                throw new InvalidOperationException(
                    "Comprehensive health multicore parity mismatch at iteration " + iteration +
                    ", issue " + index + ". Expected " + Describe(left) + ", actual " + Describe(right) + ".");
            }
        }

        private static string Describe(ModelHealthIssue issue) =>
            "[" + issue.Code + "," + issue.Severity + "," + issue.ElementId + "] " + issue.Message;

        private sealed class MultiCountSet : ISet<string>, IReadOnlyCollection<string>, ICollection
        {
            private readonly HashSet<string> _inner = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly bool _throwOnEnumeration;

            internal MultiCountSet(int genericCount, int readOnlyCount, int nonGenericCount, bool throwOnEnumeration)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _throwOnEnumeration = throwOnEnumeration;
            }

            internal bool EnumeratorRequested { get; private set; }

            int ICollection<string>.Count => _genericCount;
            int IReadOnlyCollection<string>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<string>.IsReadOnly => false;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public bool Add(string item) => _inner.Add(item);
            void ICollection<string>.Add(string item) => _inner.Add(item);
            public void ExceptWith(IEnumerable<string> other) => _inner.ExceptWith(other);
            public void IntersectWith(IEnumerable<string> other) => _inner.IntersectWith(other);
            public bool IsProperSubsetOf(IEnumerable<string> other) => _inner.IsProperSubsetOf(other);
            public bool IsProperSupersetOf(IEnumerable<string> other) => _inner.IsProperSupersetOf(other);
            public bool IsSubsetOf(IEnumerable<string> other) => _inner.IsSubsetOf(other);
            public bool IsSupersetOf(IEnumerable<string> other) => _inner.IsSupersetOf(other);
            public bool Overlaps(IEnumerable<string> other) => _inner.Overlaps(other);
            public bool SetEquals(IEnumerable<string> other) => _inner.SetEquals(other);
            public void SymmetricExceptWith(IEnumerable<string> other) => _inner.SymmetricExceptWith(other);
            public void UnionWith(IEnumerable<string> other) => _inner.UnionWith(other);
            public void Clear() => _inner.Clear();
            public bool Contains(string item) => _inner.Contains(item);
            public void CopyTo(string[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);
            void ICollection.CopyTo(Array array, int index)
            {
                foreach (var value in _inner)
                    array.SetValue(value, index++);
            }
            public bool Remove(string item) => _inner.Remove(item);

            public IEnumerator<string> GetEnumerator()
            {
                EnumeratorRequested = true;
                if (_throwOnEnumeration)
                    throw new InvalidOperationException("Enumeration should not occur for malformed known Count contracts.");
                return _inner.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
