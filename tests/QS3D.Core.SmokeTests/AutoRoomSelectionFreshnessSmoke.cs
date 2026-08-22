using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AutoRoomSelectionFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            MutatingCallerSetFailsBeforeStaleMutation();
            StableSetsStillMarkMatchingRoomStale();
        }

        private static void MutatingCallerSetFailsBeforeStaleMutation()
        {
            var project = CreateProject(out var room);
            var beforeVersion = project.ChangeVersion;
            var beforeDirty = room.Dirty;
            var active = new CallbackSet(Array.Empty<string>(), () => project.Touch());
            var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AA" };

            try
            {
                AutoRoomLifecycle.MarkStaleForSelection(
                    project,
                    active,
                    selected,
                    "F1",
                    "Z1",
                    new DateTime(2026, 8, 12, 3, 0, 0, DateTimeKind.Utc));
            }
            catch (InvalidOperationException ex)
            {
                if ((ex.Message ?? string.Empty).IndexOf("changed while Auto Room stale-selection inputs", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Auto Room selection freshness returned the wrong failure.", ex);

                Require(project.ChangeVersion == beforeVersion + 1L, "Caller-side project mutation was not preserved.");
                Require(room.Dirty == beforeDirty, "Freshness rejection dirtied the room.");
                Require(room.Properties.TryGetValue(AutoRoomLifecycle.BoundaryStateKey, out var state) &&
                        string.Equals(state, AutoRoomLifecycle.BoundaryStateActive, StringComparison.Ordinal),
                    "Freshness rejection changed the room boundary state.");
                Require(!room.Properties.ContainsKey("BoundaryStaleUtc"), "Freshness rejection wrote BoundaryStaleUtc.");
                Require(!room.Properties.ContainsKey("BoundaryStaleReason"), "Freshness rejection wrote BoundaryStaleReason.");
                return;
            }

            throw new InvalidOperationException("Auto Room stale selection accepted a project mutation during caller-set enumeration.");
        }

        private static void StableSetsStillMarkMatchingRoomStale()
        {
            var project = CreateProject(out var room);
            var beforeVersion = project.ChangeVersion;
            var stale = AutoRoomLifecycle.MarkStaleForSelection(
                project,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AA" },
                "F1",
                "Z1",
                new DateTime(2026, 8, 12, 3, 0, 0, DateTimeKind.Utc));

            Require(stale.Count == 1 && ReferenceEquals(stale[0], room), "Stable Auto Room selection did not return the matching stale room.");
            Require(AutoRoomLifecycle.IsStaleAutoRoom(room), "Stable Auto Room selection did not mark the room stale.");
            Require(room.Properties.TryGetValue("BoundaryStaleReason", out var reason) && string.Equals(reason, "TopologyChanged", StringComparison.Ordinal),
                "Stable Auto Room selection did not write the stale reason.");
            Require(project.ChangeVersion == beforeVersion + 1L, "Stable Auto Room stale mutation must advance ChangeVersion exactly once.");
        }

        private static ProjectState CreateProject(out ProjectElement room)
        {
            var project = new ProjectState("AUTO-ROOM-FRESH", "Auto Room selection freshness");
            room = new ProjectElement("ROOM-1", ElementCategory.Room, string.Empty, "F1", "Z1");
            room.Properties[AutoRoomLifecycle.BoundaryModeKey] = AutoRoomLifecycle.BoundaryModeAutoNetwork;
            room.Properties[AutoRoomLifecycle.BoundaryStateKey] = AutoRoomLifecycle.BoundaryStateActive;
            room.Properties[AutoRoomLifecycle.BoundarySourceSignatureKey] = "AA";
            room.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(room);
            return project;
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private sealed class CallbackSet : ISet<string>
        {
            private readonly HashSet<string> _inner;
            private readonly Action _onEnumerate;

            internal CallbackSet(IEnumerable<string> values, Action onEnumerate)
            {
                _inner = new HashSet<string>(values ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
                _onEnumerate = onEnumerate ?? throw new ArgumentNullException(nameof(onEnumerate));
            }

            public IEnumerator<string> GetEnumerator()
            {
                _onEnumerate();
                return _inner.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public int Count => _inner.Count;
            public bool IsReadOnly => false;
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
            public bool Remove(string item) => _inner.Remove(item);
        }
    }
}
