using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectPersistenceCheckpointCountStabilitySmoke
    {
        internal static void Run()
        {
            RejectsPostTraversalCountDrift();
            RejectsPostTraversalInterfaceConflict();
            RejectsPostTraversalNegativeCount();
            RejectsPostTraversalOversizedCount();
            StableCountedInputRemainsAccepted();
            UnknownCountStreamingInputRemainsSinglePass();
        }

        private static void RejectsPostTraversalCountDrift()
        {
            var project = CreateProject();
            var source = new DriftingReadOnlyCollection<string>(2, 3, "E1", "E2");
            AssertInvalid(() => ProjectPersistenceCheckpoint.Capture(project, source), "post-traversal Count drift");
        }

        private static void RejectsPostTraversalInterfaceConflict()
        {
            var project = CreateProject();
            var source = new DriftingMultiCountCollection<string>(
                beforeGenericCount: 2,
                beforeReadOnlyCount: 2,
                beforeNonGenericCount: 2,
                afterGenericCount: 2,
                afterReadOnlyCount: 3,
                afterNonGenericCount: 2,
                "E1",
                "E2");
            AssertInvalid(() => ProjectPersistenceCheckpoint.Capture(project, source), "post-traversal Count interface conflict");
        }

        private static void RejectsPostTraversalNegativeCount()
        {
            var project = CreateProject();
            var source = new DriftingReadOnlyCollection<string>(2, -1, "E1", "E2");
            AssertInvalid(() => ProjectPersistenceCheckpoint.Capture(project, source), "post-traversal negative Count");
        }

        private static void RejectsPostTraversalOversizedCount()
        {
            var project = CreateProject();
            var source = new DriftingReadOnlyCollection<string>(2, 10001, "E1", "E2");
            AssertInvalid(() => ProjectPersistenceCheckpoint.Capture(project, source), "post-traversal oversized Count");
        }

        private static void StableCountedInputRemainsAccepted()
        {
            var project = CreateProject();
            var source = new DriftingMultiCountCollection<string>(2, 2, 2, 2, 2, 2, "E1", "E2");
            var checkpoint = ProjectPersistenceCheckpoint.Capture(project, source);
            Equal(2, checkpoint.ElementIds.Count, "Stable counted input must remain accepted.");
        }

        private static void UnknownCountStreamingInputRemainsSinglePass()
        {
            var project = CreateProject();
            var source = new SinglePassEnumerable<string>("E1", "E2");
            var checkpoint = ProjectPersistenceCheckpoint.Capture(project, source);
            Equal(2, checkpoint.ElementIds.Count, "Unknown-count streaming input must remain accepted.");
            Equal(1, source.EnumerationCount, "Unknown-count streaming input must not be replayed for Count stability.");
        }

        private static ProjectState CreateProject()
        {
            var project = new ProjectState("P-CHECKPOINT-STABILITY", "Checkpoint stability");
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Beam));
            project.Elements.Add(new ProjectElement("E2", ElementCategory.Column));
            return project;
        }

        private static void AssertInvalid(Action action, string scenario)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new InvalidOperationException("Persistence checkpoint must reject " + scenario + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class DriftingReadOnlyCollection<T> : IReadOnlyCollection<T>
        {
            private readonly T[] _items;
            private readonly int _beforeCount;
            private readonly int _afterCount;
            private bool _traversed;

            internal DriftingReadOnlyCollection(int beforeCount, int afterCount, params T[] items)
            {
                _beforeCount = beforeCount;
                _afterCount = afterCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count => _traversed ? _afterCount : _beforeCount;

            public IEnumerator<T> GetEnumerator()
            {
                for (var i = 0; i < _items.Length; i++)
                    yield return _items[i];
                _traversed = true;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class DriftingMultiCountCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly T[] _items;
            private readonly int _beforeGenericCount;
            private readonly int _beforeReadOnlyCount;
            private readonly int _beforeNonGenericCount;
            private readonly int _afterGenericCount;
            private readonly int _afterReadOnlyCount;
            private readonly int _afterNonGenericCount;
            private bool _traversed;

            internal DriftingMultiCountCollection(
                int beforeGenericCount,
                int beforeReadOnlyCount,
                int beforeNonGenericCount,
                int afterGenericCount,
                int afterReadOnlyCount,
                int afterNonGenericCount,
                params T[] items)
            {
                _beforeGenericCount = beforeGenericCount;
                _beforeReadOnlyCount = beforeReadOnlyCount;
                _beforeNonGenericCount = beforeNonGenericCount;
                _afterGenericCount = afterGenericCount;
                _afterReadOnlyCount = afterReadOnlyCount;
                _afterNonGenericCount = afterNonGenericCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            int ICollection<T>.Count => _traversed ? _afterGenericCount : _beforeGenericCount;
            int IReadOnlyCollection<T>.Count => _traversed ? _afterReadOnlyCount : _beforeReadOnlyCount;
            int ICollection.Count => _traversed ? _afterNonGenericCount : _beforeNonGenericCount;
            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<T> GetEnumerator()
            {
                for (var i = 0; i < _items.Length; i++)
                    yield return _items[i];
                _traversed = true;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => _items.CopyTo(array, index);
        }

        private sealed class SinglePassEnumerable<T> : IEnumerable<T>
        {
            private readonly T[] _items;

            internal SinglePassEnumerable(params T[] items)
            {
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            internal int EnumerationCount { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                EnumerationCount++;
                if (EnumerationCount > 1)
                    throw new InvalidOperationException("Streaming source was enumerated more than once.");
                return ((IEnumerable<T>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    internal static class ProjectPersistenceCheckpointCountStabilityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ProjectPersistenceCheckpointCountStabilitySmoke.Run();
        }
    }
}
