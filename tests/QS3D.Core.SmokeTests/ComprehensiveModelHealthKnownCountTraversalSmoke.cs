using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ComprehensiveModelHealthKnownCountTraversalSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsSourceUnderEnumeration();
            RejectsGeneratedOverEnumeration();
            AcceptsHonestCountEvenWhenNormalizationDeduplicates();
        }

        private static void RejectsSourceUnderEnumeration()
        {
            var service = new ComprehensiveModelHealthService(1);
            var source = new CountedTraversalSet(2, new[] { "SRC-A" });

            ThrowsMismatch(
                () => service.Inspect(NewProject(), source, null),
                source,
                "source");
        }

        private static void RejectsGeneratedOverEnumeration()
        {
            var service = new ComprehensiveModelHealthService(1);
            var source = new CountedTraversalSet(1, new[] { "GEN-A", "GEN-B" });

            ThrowsMismatch(
                () => service.Inspect(NewProject(), null, source),
                source,
                "generated-solid");
        }

        private static void AcceptsHonestCountEvenWhenNormalizationDeduplicates()
        {
            var service = new ComprehensiveModelHealthService(1);
            var source = new CountedTraversalSet(2, new[] { " ABC ", "abc" });

            _ = service.Inspect(NewProject(), source, null);

            Equal(1, source.EnumeratorCalls, "Honest counted source must be traversed once.");
            Equal(2, source.YieldedCount, "Count/traversal validation must use raw traversal cardinality before normalization/deduplication.");
        }

        private static void ThrowsMismatch(Action action, CountedTraversalSet source, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(label, StringComparison.OrdinalIgnoreCase) < 0 ||
                    ex.Message.IndexOf("Count contract does not match enumerated Handle count", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Unexpected comprehensive model-health Count/traversal diagnostic: " + ex.Message);

                Equal(1, source.EnumeratorCalls, "Count/traversal mismatch must consume the input exactly once.");
                return;
            }

            throw new InvalidOperationException("Expected comprehensive model-health Count/traversal mismatch rejection.");
        }

        private static ProjectState NewProject()
        {
            var project = new ProjectState(Guid.NewGuid().ToString("N"), "Count traversal health");
            project.Zones.Add(new ZoneDefinition("zone-1", "Zone 1"));
            project.Floors.Add(new FloorDefinition("floor-0", "Floor 0", 0d));
            project.ActiveZoneId = "zone-1";
            project.ActiveFloorId = "floor-0";
            return project;
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class CountedTraversalSet : ISet<string>, IReadOnlyCollection<string>, ICollection
        {
            private readonly int _knownCount;
            private readonly IReadOnlyList<string> _values;

            internal CountedTraversalSet(int knownCount, IReadOnlyList<string> values)
            {
                _knownCount = knownCount;
                _values = values ?? throw new ArgumentNullException(nameof(values));
            }

            internal int EnumeratorCalls { get; private set; }
            internal int YieldedCount { get; private set; }

            int ICollection<string>.Count => _knownCount;
            int IReadOnlyCollection<string>.Count => _knownCount;
            int ICollection.Count => _knownCount;
            bool ICollection<string>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<string> GetEnumerator()
            {
                EnumeratorCalls++;
                if (EnumeratorCalls > 1)
                    throw new InvalidOperationException("Comprehensive health input was enumerated more than once.");
                return Enumerate().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private IEnumerable<string> Enumerate()
            {
                for (var index = 0; index < _values.Count; index++)
                {
                    YieldedCount++;
                    yield return _values[index];
                }
            }

            public bool Contains(string item)
            {
                for (var index = 0; index < _values.Count; index++)
                    if (string.Equals(_values[index], item, StringComparison.OrdinalIgnoreCase)) return true;
                return false;
            }

            public void CopyTo(string[] array, int arrayIndex)
            {
                for (var index = 0; index < _values.Count; index++)
                    array[arrayIndex + index] = _values[index];
            }

            void ICollection.CopyTo(Array array, int index)
            {
                for (var valueIndex = 0; valueIndex < _values.Count; valueIndex++)
                    array.SetValue(_values[valueIndex], index + valueIndex);
            }

            public bool IsSubsetOf(IEnumerable<string> other) => Snapshot().IsSubsetOf(other);
            public bool IsSupersetOf(IEnumerable<string> other) => Snapshot().IsSupersetOf(other);
            public bool IsProperSupersetOf(IEnumerable<string> other) => Snapshot().IsProperSupersetOf(other);
            public bool IsProperSubsetOf(IEnumerable<string> other) => Snapshot().IsProperSubsetOf(other);
            public bool Overlaps(IEnumerable<string> other) => Snapshot().Overlaps(other);
            public bool SetEquals(IEnumerable<string> other) => Snapshot().SetEquals(other);

            private HashSet<string> Snapshot() => new HashSet<string>(_values, StringComparer.OrdinalIgnoreCase);

            public bool Add(string item) => throw new NotSupportedException();
            void ICollection<string>.Add(string item) => throw new NotSupportedException();
            public void ExceptWith(IEnumerable<string> other) => throw new NotSupportedException();
            public void IntersectWith(IEnumerable<string> other) => throw new NotSupportedException();
            public void SymmetricExceptWith(IEnumerable<string> other) => throw new NotSupportedException();
            public void UnionWith(IEnumerable<string> other) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
        }
    }
}
