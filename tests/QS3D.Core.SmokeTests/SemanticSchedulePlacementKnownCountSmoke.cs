using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSchedulePlacementKnownCountSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsInvalidKnownCountsBeforeEnumeration();
            RejectsKnownCountTraversalMismatch();
            AcceptsMatchingKnownCounts();
            AcceptsPureStreamingInputs();
        }

        private static void RejectsInvalidKnownCountsBeforeEnumeration()
        {
            var sheet = EmptySheet();
            var schedule = Schedule("SCH-1");
            var item = new SemanticSchedulePlacementItem("SCH-1", 50d, 30d);

            var negativeSchedules = new MultiCountSource<SemanticScheduleDefinition>(
                -1, -1, -1, throwOnEnumeration: true, schedule);
            ExpectInvalidOperation(
                () => SemanticSchedulePlacementPlanner.Build(sheet, negativeSchedules, new[] { item }),
                "negative known count");
            if (negativeSchedules.Enumerated)
                throw new InvalidOperationException("Negative available-schedule Count must fail before enumeration.");

            var oversizedItems = new MultiCountSource<SemanticSchedulePlacementItem>(
                129, 129, 129, throwOnEnumeration: true, item);
            ExpectInvalidOperation(
                () => SemanticSchedulePlacementPlanner.Build(sheet, new[] { schedule }, oversizedItems),
                "at most 128 schedules per sheet");
            if (oversizedItems.Enumerated)
                throw new InvalidOperationException("Oversized placement-item Count must fail before enumeration.");

            var conflictingSchedules = new MultiCountSource<SemanticScheduleDefinition>(
                1, 2, 1, throwOnEnumeration: true, schedule);
            ExpectInvalidOperation(
                () => SemanticSchedulePlacementPlanner.Build(sheet, conflictingSchedules, new[] { item }),
                "conflicting known counts");
            if (conflictingSchedules.Enumerated)
                throw new InvalidOperationException("Conflicting available-schedule Counts must fail before enumeration.");

            var conflictingItems = new MultiCountSource<SemanticSchedulePlacementItem>(
                1, 2, 1, throwOnEnumeration: true, item);
            ExpectInvalidOperation(
                () => SemanticSchedulePlacementPlanner.Build(sheet, new[] { schedule }, conflictingItems),
                "conflicting known counts");
            if (conflictingItems.Enumerated)
                throw new InvalidOperationException("Conflicting placement-item Counts must fail before enumeration.");
        }

        private static void RejectsKnownCountTraversalMismatch()
        {
            var sheet = EmptySheet();
            var schedule = Schedule("SCH-1");
            var item = new SemanticSchedulePlacementItem("SCH-1", 50d, 30d);

            var shortSchedules = new MultiCountSource<SemanticScheduleDefinition>(
                2, 2, 2, throwOnEnumeration: false, schedule);
            ExpectInvalidOperation(
                () => SemanticSchedulePlacementPlanner.Build(sheet, shortSchedules, new[] { item }),
                "available schedule traversal count does not match");

            var longItems = new MultiCountSource<SemanticSchedulePlacementItem>(
                1, 1, 1, throwOnEnumeration: false,
                item,
                new SemanticSchedulePlacementItem("SCH-2", 40d, 20d));
            ExpectInvalidOperation(
                () => SemanticSchedulePlacementPlanner.Build(
                    sheet,
                    new[] { schedule, Schedule("SCH-2") },
                    longItems),
                "item traversal count does not match");
        }

        private static void AcceptsMatchingKnownCounts()
        {
            var schedule = Schedule("SCH-1");
            var item = new SemanticSchedulePlacementItem("SCH-1", 50d, 30d);
            var schedules = new MultiCountSource<SemanticScheduleDefinition>(
                1, 1, 1, throwOnEnumeration: false, schedule);
            var items = new MultiCountSource<SemanticSchedulePlacementItem>(
                1, 1, 1, throwOnEnumeration: false, item);

            var plan = SemanticSchedulePlacementPlanner.Build(EmptySheet(), schedules, items);
            if (plan.Placements.Count != 1 || plan.Placements[0].ScheduleId != "SCH-1")
                throw new InvalidOperationException("Matching schedule-placement Count contracts should remain accepted.");
        }

        private static void AcceptsPureStreamingInputs()
        {
            var schedules = new StreamingSource<SemanticScheduleDefinition>(Schedule("SCH-1"));
            var items = new StreamingSource<SemanticSchedulePlacementItem>(
                new SemanticSchedulePlacementItem("SCH-1", 50d, 30d));

            var plan = SemanticSchedulePlacementPlanner.Build(EmptySheet(), schedules, items);
            if (plan.Placements.Count != 1 || plan.Placements[0].ScheduleId != "SCH-1")
                throw new InvalidOperationException("Pure streaming schedule-placement inputs should remain supported.");
        }

        private static SemanticSheetPlan EmptySheet()
        {
            return SemanticSheetPlanner.Build(
                new SemanticSheetDefinition(
                    "S-KNOWN-COUNT",
                    "A-KNOWN-COUNT",
                    "Known Count Schedule Sheet",
                    297d,
                    210d,
                    Array.Empty<SemanticSheetPlacementDefinition>()),
                Array.Empty<SemanticViewPlan>());
        }

        private static SemanticScheduleDefinition Schedule(string id)
        {
            return new SemanticScheduleDefinition(
                id,
                "Schedule " + id,
                "Schedule " + id,
                Array.Empty<ElementCategory>(),
                string.Empty,
                string.Empty,
                Array.Empty<string>(),
                Array.Empty<string>(),
                new[] { new SemanticDocumentationColumn("ID", "{Id}") });
        }

        private static void ExpectInvalidOperation(Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex) when (
                ex.Message.IndexOf(expectedMessage, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return;
            }

            throw new InvalidOperationException(
                "Expected schedule-placement failure containing: " + expectedMessage + ".");
        }

        private sealed class MultiCountSource<T> :
            ICollection<T>,
            IReadOnlyCollection<T>,
            ICollection
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly bool _throwOnEnumeration;
            private readonly T[] _items;

            public MultiCountSource(
                int genericCount,
                int readOnlyCount,
                int nonGenericCount,
                bool throwOnEnumeration,
                params T[] items)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _throwOnEnumeration = throwOnEnumeration;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public bool Enumerated { get; private set; }
            int ICollection<T>.Count => _genericCount;
            int IReadOnlyCollection<T>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<T> GetEnumerator()
            {
                Enumerated = true;
                if (_throwOnEnumeration)
                    throw new InvalidOperationException("Known Count contract should have failed before enumeration.");
                return ((IEnumerable<T>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => Array.IndexOf(_items, item) >= 0;
            public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            void ICollection.CopyTo(Array array, int index) => _items.CopyTo(array, index);
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

        private sealed class StreamingSource<T> : IEnumerable<T>
        {
            private readonly T[] _items;

            public StreamingSource(params T[] items)
            {
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
