using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AutoRoomStaleSelectionBoundSmoke
    {
        private const int MaximumSourceHandles = 5000;

        internal static void Run()
        {
            KnownOversizeFailsBeforeEnumerationOrMutation();
            DishonestCountStopsAtFirstDisallowedEntry();
            ExactBoundaryRemainsAccepted();
        }

        private static void KnownOversizeFailsBeforeEnumerationOrMutation()
        {
            var project = NewProject();
            var handles = new ProbeSet(
                reportedCount: MaximumSourceHandles + 1,
                yieldedCount: MaximumSourceHandles + 1,
                failIfEnumerated: true);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;

            var error = Capture<InvalidOperationException>(() => AutoRoomLifecycle.MarkStaleForSelection(
                project,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                handles,
                "f",
                "z",
                new DateTime(2026, 8, 16, 15, 0, 0, DateTimeKind.Utc)));

            Contains("cannot exceed 5000", error.Message, "Known oversize stale-selection input must report the existing Auto Room source-handle bound.");
            Equal(0, handles.ObservedEntries, "Known oversize stale-selection input must fail from Count before enumeration.");
            Equal(beforeVersion, project.ChangeVersion, "Known oversize stale-selection input must fail before project mutation.");
            Equal(beforeUpdatedUtc, project.UpdatedUtc, "Known oversize stale-selection input must preserve project timestamps.");
        }

        private static void DishonestCountStopsAtFirstDisallowedEntry()
        {
            var project = NewProject();
            var handles = new ProbeSet(
                reportedCount: 1,
                yieldedCount: MaximumSourceHandles + 2,
                failIfEnumerated: false);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;

            var error = Capture<InvalidOperationException>(() => AutoRoomLifecycle.MarkStaleForSelection(
                project,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                handles,
                "f",
                "z",
                new DateTime(2026, 8, 16, 15, 1, 0, DateTimeKind.Utc)));

            Contains("cannot exceed 5000", error.Message, "Dishonest Count must not evade the streaming source-handle bound.");
            Equal(MaximumSourceHandles + 1, handles.ObservedEntries, "Streaming enforcement must stop on entry 5001 and must not consume entry 5002.");
            Equal(beforeVersion, project.ChangeVersion, "Dishonest-count rejection must happen before project mutation.");
            Equal(beforeUpdatedUtc, project.UpdatedUtc, "Dishonest-count rejection must preserve project timestamps.");
        }

        private static void ExactBoundaryRemainsAccepted()
        {
            var project = NewProject();
            var handles = new ProbeSet(
                reportedCount: MaximumSourceHandles,
                yieldedCount: MaximumSourceHandles,
                failIfEnumerated: false);
            var beforeVersion = project.ChangeVersion;

            var stale = AutoRoomLifecycle.MarkStaleForSelection(
                project,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                handles,
                "f",
                "z",
                new DateTime(2026, 8, 16, 15, 2, 0, DateTimeKind.Utc));

            Equal(0, stale.Count, "Exactly 5000 stale-selection source handles must remain accepted.");
            Equal(MaximumSourceHandles, handles.ObservedEntries, "Exactly 5000 entries must be fully consumed.");
            Equal(beforeVersion, project.ChangeVersion, "A boundary-sized selection with no matching rooms must not mutate the project.");
        }

        private static ProjectState NewProject()
        {
            var project = new ProjectState("p-stale-bound", "AutoRoom stale selection bound");
            project.Floors.Add(new FloorDefinition("f", "Floor", 0d));
            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            project.ActiveFloorId = "f";
            project.ActiveZoneId = "z";
            project.Families.Add(new ProjectFamily("room", "Room", ElementCategory.Room));
            return project;
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new Exception("Expected " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new Exception(message + " Actual: " + (actual ?? "<null>"));
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected: " + expected + "; actual: " + actual + ".");
        }

        private sealed class ProbeSet : ISet<string>
        {
            private readonly int _reportedCount;
            private readonly int _yieldedCount;
            private readonly bool _failIfEnumerated;

            internal ProbeSet(int reportedCount, int yieldedCount, bool failIfEnumerated)
            {
                _reportedCount = reportedCount;
                _yieldedCount = yieldedCount;
                _failIfEnumerated = failIfEnumerated;
            }

            internal int ObservedEntries { get; private set; }

            public int Count => _reportedCount;
            public bool IsReadOnly => true;

            public IEnumerator<string> GetEnumerator()
            {
                if (_failIfEnumerated)
                    throw new Exception("Oversize set must not be enumerated.");

                for (var index = 0; index < _yieldedCount; index++)
                {
                    ObservedEntries++;
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
            public bool Add(string item) => throw new NotSupportedException();
            void ICollection<string>.Add(string item) => throw new NotSupportedException();
            public void ExceptWith(IEnumerable<string> other) => throw new NotSupportedException();
            public void IntersectWith(IEnumerable<string> other) => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
            public void SymmetricExceptWith(IEnumerable<string> other) => throw new NotSupportedException();
            public void UnionWith(IEnumerable<string> other) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
        }
    }
}
