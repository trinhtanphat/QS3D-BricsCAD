using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SelectionStateSmoke
    {
        public static void Run()
        {
            ReplaceTrimsDeduplicatesAndIgnoresBlankIds();
            CanonicallyEquivalentReplaceDoesNotRaiseChanged();
            ElementIdsAreDeterministicAndDoNotLeakMutableState();
            ClearRaisesOnlyWhenStateChanges();
            DishonestKnownCountsFailClosedAndPreserveState();
            NonGenericKnownCountsFailClosedBeforeEnumeration();
            NonGenericKnownCountTraversalMismatchFailsClosed();
            ConflictingKnownCountsFailBeforeEnumeration();
            ExactKnownCountAndStreamingInputsRemainAccepted();
        }

        private static void ReplaceTrimsDeduplicatesAndIgnoresBlankIds()
        {
            var state = new SelectionState();
            var changed = 0;
            state.Changed += (_, __) => changed++;
            state.Replace(new[] { " A ", "a", " B", "   " });

            if (changed != 1) throw new Exception("Canonical selection replace must raise exactly one change event.");
            var ids = state.ElementIds.ToArray();
            if (!ids.SequenceEqual(new[] { "A", "B" }, StringComparer.OrdinalIgnoreCase))
                throw new Exception("Selection state must trim, de-duplicate case-insensitively, ignore blanks and expose deterministic ordering.");
            if (ids.Any(id => id != id.Trim())) throw new Exception("Selection state must never expose padded semantic IDs.");
        }

        private static void CanonicallyEquivalentReplaceDoesNotRaiseChanged()
        {
            var state = new SelectionState();
            state.Replace(new[] { "A", "B" });
            var changed = 0;
            state.Changed += (_, __) => changed++;
            state.Replace(new[] { " b ", " A ", "a" });
            if (changed != 0) throw new Exception("Canonical-equivalent selection replace must not raise Changed.");
        }

        private static void ElementIdsAreDeterministicAndDoNotLeakMutableState()
        {
            var state = new SelectionState();
            state.Replace(new[] { "Z", "a", "M" });
            var exposed = state.ElementIds;
            if (exposed is HashSet<string>) throw new Exception("Selection state must not expose its mutable HashSet implementation.");
            var ids = exposed.ToArray();
            if (!ids.SequenceEqual(new[] { "a", "M", "Z" }, StringComparer.OrdinalIgnoreCase))
                throw new Exception("Selection state enumeration must be deterministic and case-insensitively ordered.");

            if (exposed is string[] snapshot && snapshot.Length > 0) snapshot[0] = "MUTATED";
            var after = state.ElementIds.ToArray();
            if (!after.SequenceEqual(new[] { "a", "M", "Z" }, StringComparer.OrdinalIgnoreCase))
                throw new Exception("Mutating an exposed selection snapshot must not mutate internal selection state.");
        }

        private static void ClearRaisesOnlyWhenStateChanges()
        {
            var state = new SelectionState();
            var changed = 0;
            state.Changed += (_, __) => changed++;
            state.Clear();
            if (changed != 0) throw new Exception("Clearing an empty selection must not raise Changed.");
            state.Replace(new[] { "A" });
            state.Clear();
            state.Clear();
            if (changed != 2) throw new Exception("Selection replace + first clear must raise two total changes; repeated empty clear must be silent.");
        }

        private static void DishonestKnownCountsFailClosedAndPreserveState()
        {
            var state = new SelectionState();
            state.Replace(new[] { "KEEP" });
            var changed = 0;
            state.Changed += (_, __) => changed++;

            var negative = new MisreportedReadOnlyCollection(-1, true, "A");
            ExpectKnownCountFailure(() => state.Replace(negative), "Negative known Count must fail closed before enumeration.");
            if (negative.EnumerationCount != 0)
                throw new Exception("Negative semantic-selection Count must be rejected before enumeration.");

            ExpectKnownCountFailure(
                () => state.Replace(new MisreportedReadOnlyCollection(2, false, "A")),
                "Under-yielding semantic selection must reject a Count/traversal mismatch.");
            ExpectKnownCountFailure(
                () => state.Replace(new MisreportedReadOnlyCollection(1, false, "A", "B")),
                "Over-yielding semantic selection must reject a Count/traversal mismatch.");

            if (changed != 0)
                throw new Exception("Rejected semantic-selection known Count contracts must not raise Changed.");
            if (!state.ElementIds.SequenceEqual(new[] { "KEEP" }, StringComparer.OrdinalIgnoreCase))
                throw new Exception("Rejected semantic-selection known Count contracts must preserve the prior selection atomically.");
        }

        private static void NonGenericKnownCountsFailClosedBeforeEnumeration()
        {
            var state = new SelectionState();
            state.Replace(new[] { "KEEP" });

            var negative = new NonGenericKnownCountCollection(-1, true, "A");
            ExpectKnownCountFailure(
                () => state.Replace(negative),
                "Negative non-generic semantic-selection Count must fail closed before enumeration.");
            if (negative.EnumerationCount != 0)
                throw new Exception("Negative non-generic semantic-selection Count must be rejected before enumeration.");

            var oversized = new NonGenericKnownCountCollection(10001, true, "A");
            try
            {
                state.Replace(oversized);
            }
            catch (InvalidOperationException)
            {
                if (oversized.EnumerationCount != 0)
                    throw new Exception("Oversized non-generic semantic-selection Count must be rejected before enumeration.");
                if (!state.ElementIds.SequenceEqual(new[] { "KEEP" }, StringComparer.OrdinalIgnoreCase))
                    throw new Exception("Rejected oversized non-generic Count must preserve selection state.");
                return;
            }

            throw new Exception("Oversized non-generic semantic-selection Count must fail closed before enumeration.");
        }

        private static void NonGenericKnownCountTraversalMismatchFailsClosed()
        {
            var state = new SelectionState();
            state.Replace(new[] { "KEEP" });
            var source = new NonGenericKnownCountCollection(2, false, "A");

            ExpectKnownCountFailure(
                () => state.Replace(source),
                "Non-generic semantic-selection Count/traversal mismatch must fail closed.");
            if (source.EnumerationCount != 1)
                throw new Exception("Valid non-generic Count must allow exactly one source enumeration before mismatch validation.");
            if (!state.ElementIds.SequenceEqual(new[] { "KEEP" }, StringComparer.OrdinalIgnoreCase))
                throw new Exception("Non-generic Count/traversal mismatch must preserve selection state.");
        }

        private static void ConflictingKnownCountsFailBeforeEnumeration()
        {
            var state = new SelectionState();
            state.Replace(new[] { "KEEP" });

            var genericConflict = new ConflictingKnownCountCollection(1, 2, 1);
            ExpectKnownCountFailure(
                () => state.Replace(genericConflict),
                "Conflicting ICollection/IReadOnlyCollection Counts must fail closed.");
            if (genericConflict.EnumerationCount != 0)
                throw new Exception("Conflicting semantic-selection generic/read-only Counts must be rejected before enumeration.");

            var nonGenericConflict = new ConflictingKnownCountCollection(1, 1, 2);
            ExpectKnownCountFailure(
                () => state.Replace(nonGenericConflict),
                "Conflicting generic/non-generic semantic-selection Counts must fail closed.");
            if (nonGenericConflict.EnumerationCount != 0)
                throw new Exception("Conflicting semantic-selection non-generic Count must be rejected before enumeration.");

            if (!state.ElementIds.SequenceEqual(new[] { "KEEP" }, StringComparer.OrdinalIgnoreCase))
                throw new Exception("Conflicting known Counts must not mutate semantic selection state.");
        }

        private static void ExactKnownCountAndStreamingInputsRemainAccepted()
        {
            var state = new SelectionState();
            state.Replace(new MisreportedReadOnlyCollection(2, false, " A ", "B"));
            if (!state.ElementIds.SequenceEqual(new[] { "A", "B" }, StringComparer.OrdinalIgnoreCase))
                throw new Exception("Exact semantic-selection known Count/traversal agreement must remain accepted.");

            var nonGeneric = new NonGenericKnownCountCollection(2, false, " E ", "F");
            state.Replace(nonGeneric);
            if (nonGeneric.EnumerationCount != 1 ||
                !state.ElementIds.SequenceEqual(new[] { "E", "F" }, StringComparer.OrdinalIgnoreCase))
                throw new Exception("Exact non-generic semantic-selection Count/traversal agreement must remain accepted.");

            state.Replace(Stream(" C ", "D"));
            if (!state.ElementIds.SequenceEqual(new[] { "C", "D" }, StringComparer.OrdinalIgnoreCase))
                throw new Exception("Pure streaming semantic-selection input must remain accepted without a known Count.");
        }

        private static IEnumerable<string> Stream(params string[] ids)
        {
            for (var i = 0; i < ids.Length; i++) yield return ids[i];
        }

        private static void ExpectKnownCountFailure(Action action, string message)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("Count", StringComparison.Ordinal) >= 0) return;
                throw new Exception(message + " Unexpected error: " + ex.Message, ex);
            }

            throw new Exception(message);
        }

        private sealed class MisreportedReadOnlyCollection : IReadOnlyCollection<string>
        {
            private readonly string[] _items;
            private readonly bool _throwOnEnumeration;

            internal MisreportedReadOnlyCollection(int count, bool throwOnEnumeration, params string[] items)
            {
                Count = count;
                _throwOnEnumeration = throwOnEnumeration;
                _items = items ?? Array.Empty<string>();
            }

            public int Count { get; }
            internal int EnumerationCount { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                EnumerationCount++;
                if (_throwOnEnumeration)
                    throw new Exception("Semantic selection must not enumerate an invalid known Count.");
                return ((IEnumerable<string>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class NonGenericKnownCountCollection : IEnumerable<string>, ICollection
        {
            private readonly int _count;
            private readonly string[] _items;
            private readonly bool _throwOnEnumeration;

            internal NonGenericKnownCountCollection(int count, bool throwOnEnumeration, params string[] items)
            {
                _count = count;
                _throwOnEnumeration = throwOnEnumeration;
                _items = items ?? Array.Empty<string>();
            }

            int ICollection.Count => _count;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            internal int EnumerationCount { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                EnumerationCount++;
                if (_throwOnEnumeration)
                    throw new Exception("Semantic selection must not enumerate an invalid non-generic known Count.");
                return ((IEnumerable<string>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection.CopyTo(Array array, int index)
            {
                for (var i = 0; i < _items.Length; i++) array.SetValue(_items[i], index + i);
            }
        }

        private sealed class ConflictingKnownCountCollection : ICollection<string>, IReadOnlyCollection<string>, ICollection
        {
            private readonly int _collectionCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;

            internal ConflictingKnownCountCollection(int collectionCount, int readOnlyCount, int nonGenericCount)
            {
                _collectionCount = collectionCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
            }

            int ICollection<string>.Count => _collectionCount;
            int IReadOnlyCollection<string>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<string>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            internal int EnumerationCount { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                EnumerationCount++;
                throw new Exception("Conflicting semantic-selection known Counts must fail before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<string>.Add(string item) => throw new NotSupportedException();
            void ICollection<string>.Clear() => throw new NotSupportedException();
            bool ICollection<string>.Contains(string item) => false;
            void ICollection<string>.CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<string>.Remove(string item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        }
    }
}
