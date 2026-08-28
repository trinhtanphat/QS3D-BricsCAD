using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserWorkspaceStateCountContractSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            OversizedKnownCountsFailBeforeEnumeration();
            InvalidAndConflictingKnownCountsFailBeforeEnumeration();
            KnownCountOverrunWinsBeforeSemanticProcessing();
            KnownCountTraversalMismatchFailsClosed();
            PureStreamingInputRetainsTraversalCap();
            HonestCountedInputsPreserveCanonicalState();
        }

        private static void OversizedKnownCountsFailBeforeEnumeration()
        {
            AssertPreEnumerationRejection(
                new CountedCollection<ElementCategory>(Array.Empty<ElementCategory>(), 10001, 10001, 10001),
                values => new ProjectBrowserWorkspaceState(categories: values));
            AssertPreEnumerationRejection(
                new CountedCollection<string>(Array.Empty<string>(), 10001, 10001, 10001),
                values => new ProjectBrowserWorkspaceState(floorIds: values));
            AssertPreEnumerationRejection(
                new CountedCollection<string>(Array.Empty<string>(), 10001, 10001, 10001),
                values => new ProjectBrowserWorkspaceState(zoneIds: values));
            AssertPreEnumerationRejection(
                new CountedCollection<string>(Array.Empty<string>(), 50001, 50001, 50001),
                values => new ProjectBrowserWorkspaceState(expandedPaths: values));
            AssertPreEnumerationRejection(
                new CountedCollection<string>(Array.Empty<string>(), 10001, 10001, 10001),
                values => new ProjectBrowserWorkspaceState(selectedElementIds: values));
        }

        private static void InvalidAndConflictingKnownCountsFailBeforeEnumeration()
        {
            var negative = new CountedCollection<string>(Array.Empty<string>(), -1, -1, -1);
            Throws<InvalidOperationException>(() => new ProjectBrowserWorkspaceState(floorIds: negative));
            Equal(0, negative.EnumerationRequests, "Negative Count must fail before GetEnumerator().");

            var conflicting = new CountedCollection<string>(Array.Empty<string>(), 1, 2, 1);
            Throws<InvalidOperationException>(() => new ProjectBrowserWorkspaceState(zoneIds: conflicting));
            Equal(0, conflicting.EnumerationRequests, "Conflicting Count contracts must fail before GetEnumerator().");
        }

        private static void KnownCountOverrunWinsBeforeSemanticProcessing()
        {
            AssertCountOverrun(
                new CountedCollection<ElementCategory>(new[] { ElementCategory.Beam, (ElementCategory)int.MaxValue }, 1, 1, 1),
                values => new ProjectBrowserWorkspaceState(categories: values),
                "project browser workspace category filter");
            AssertCountOverrun(
                new CountedCollection<string>(new[] { "F-01", " F-02" }, 1, 1, 1),
                values => new ProjectBrowserWorkspaceState(floorIds: values),
                "project browser workspace floor filter");
            AssertCountOverrun(
                new CountedCollection<string>(new[] { "Z-01", " Z-02" }, 1, 1, 1),
                values => new ProjectBrowserWorkspaceState(zoneIds: values),
                "project browser workspace zone filter");
            AssertCountOverrun(
                new CountedCollection<string>(new[] { "ROOT/A", " ROOT/B" }, 1, 1, 1),
                values => new ProjectBrowserWorkspaceState(expandedPaths: values),
                "project browser workspace expanded path");
            AssertCountOverrun(
                new CountedCollection<string>(new[] { "E-01", " E-02" }, 1, 1, 1),
                values => new ProjectBrowserWorkspaceState(selectedElementIds: values),
                "project browser workspace selected element");
        }

        private static void KnownCountTraversalMismatchFailsClosed()
        {
            var underEnumerated = new CountedCollection<string>(new[] { "F-01" }, 2, 2, 2);
            Throws<InvalidOperationException>(() => new ProjectBrowserWorkspaceState(floorIds: underEnumerated));
            Equal(1, underEnumerated.EnumerationRequests, "Count/traversal mismatch must enumerate only the input traversal once.");

            var overEnumerated = new CountedCollection<string>(new[] { "PATH-A", "PATH-B" }, 1, 1, 1);
            Throws<InvalidOperationException>(() => new ProjectBrowserWorkspaceState(expandedPaths: overEnumerated));
            Equal(1, overEnumerated.EnumerationRequests, "Count/traversal mismatch must fail during one bounded traversal.");
        }

        private static void PureStreamingInputRetainsTraversalCap()
        {
            var streaming = new StreamingEnumerable<string>(10001, i => "E-" + i.ToString("D5"));
            Throws<InvalidOperationException>(() => new ProjectBrowserWorkspaceState(selectedElementIds: streaming));
            Equal(10001, streaming.YieldRequests, "Streaming input must retain the independent 10000-entry traversal cap.");
        }

        private static void HonestCountedInputsPreserveCanonicalState()
        {
            var categories = new CountedCollection<ElementCategory>(
                new[] { ElementCategory.Column, ElementCategory.Beam }, 2, 2, 2);
            var floors = new CountedCollection<string>(new[] { "F-02", "F-01" }, 2, 2, 2);
            var zones = new CountedCollection<string>(new[] { "Z-B", "Z-A" }, 2, 2, 2);
            var expanded = new CountedCollection<string>(new[] { "ROOT/Z", "ROOT/A" }, 2, 2, 2);
            var selected = new CountedCollection<string>(new[] { "E-02", "E-01" }, 2, 2, 2);

            var state = new ProjectBrowserWorkspaceState(
                categories: categories,
                floorIds: floors,
                zoneIds: zones,
                expandedPaths: expanded,
                selectedElementIds: selected,
                primaryElementId: "e-02");

            SequenceEqual(new[] { ElementCategory.Beam, ElementCategory.Column }, state.Categories, "Category ordering changed.");
            SequenceEqual(new[] { "F-01", "F-02" }, state.FloorIds, "Floor ordering changed.");
            SequenceEqual(new[] { "Z-A", "Z-B" }, state.ZoneIds, "Zone ordering changed.");
            SequenceEqual(new[] { "ROOT/A", "ROOT/Z" }, state.ExpandedPaths, "Expanded-path ordering changed.");
            SequenceEqual(new[] { "E-01", "E-02" }, state.SelectedElementIds, "Selection ordering changed.");
            Equal("E-02", state.PrimaryElementId, "Primary selection must retain the canonical selected identity.");
        }

        private static void AssertPreEnumerationRejection<T>(CountedCollection<T> values, Func<IEnumerable<T>, ProjectBrowserWorkspaceState> create)
        {
            Throws<InvalidOperationException>(() => create(values));
            Equal(0, values.EnumerationRequests, "Oversized known Count must fail before GetEnumerator().");
        }

        private static void AssertCountOverrun<T>(
            CountedCollection<T> values,
            Func<IEnumerable<T>, ProjectBrowserWorkspaceState> create,
            string label)
        {
            ThrowsWithMessage<InvalidOperationException>(
                () => create(values),
                label + " traversal exceeds declared Count 1.");
            Equal(1, values.EnumerationRequests, "Known-Count overrun must use exactly one input traversal.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }

        private static void ThrowsWithMessage<T>(Action action, string expectedMessage) where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                Equal(expectedMessage, ex.Message, "Unexpected fail-closed precedence/message.");
                return;
            }
            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", Actual=" + actual + ".");
        }

        private static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message)
        {
            if (!expected.SequenceEqual(actual)) throw new InvalidOperationException(message);
        }

        private sealed class CountedCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly IReadOnlyList<T> _items;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;

            internal CountedCollection(IReadOnlyList<T> items, int genericCount, int readOnlyCount, int nonGenericCount)
            {
                _items = items;
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
            }

            internal int EnumerationRequests { get; private set; }

            int ICollection<T>.Count => _genericCount;
            int IReadOnlyCollection<T>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<T> GetEnumerator()
            {
                EnumerationRequests++;
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => _items.Contains(item);
            void ICollection<T>.CopyTo(T[] array, int arrayIndex)
            {
                for (var i = 0; i < _items.Count; i++) array[arrayIndex + i] = _items[i];
            }
            void ICollection.CopyTo(Array array, int index)
            {
                for (var i = 0; i < _items.Count; i++) array.SetValue(_items[i], index + i);
            }
        }

        private sealed class StreamingEnumerable<T> : IEnumerable<T>
        {
            private readonly int _count;
            private readonly Func<int, T> _factory;

            internal StreamingEnumerable(int count, Func<int, T> factory)
            {
                _count = count;
                _factory = factory;
            }

            internal int YieldRequests { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                {
                    YieldRequests++;
                    yield return _factory(i);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}