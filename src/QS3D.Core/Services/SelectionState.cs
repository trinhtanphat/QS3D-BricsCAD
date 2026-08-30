using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Services
{
    public sealed class SelectionState
    {
        private const int MaxInputCount = 10000;
        private readonly HashSet<string> _ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private long _changeVersion;

        public event EventHandler? Changed;
        public IReadOnlyCollection<string> ElementIds => _ids.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();

        public void Replace(IEnumerable<string> ids)
        {
            if (ids == null) throw new ArgumentNullException(nameof(ids));
            if (ids is ICollection<string> collection && collection.Count > MaxInputCount)
                throw new InvalidOperationException("Semantic selection cannot exceed " + MaxInputCount + " input entries.");
            if (ids is IReadOnlyCollection<string> readOnlyCollection && readOnlyCollection.Count > MaxInputCount)
                throw new InvalidOperationException("Semantic selection cannot exceed " + MaxInputCount + " input entries.");
            if (ids is System.Collections.ICollection nonGenericCollection && nonGenericCollection.Count > MaxInputCount)
                throw new InvalidOperationException("Semantic selection cannot exceed " + MaxInputCount + " input entries.");
            var knownCount = ResolveKnownCount(ids);

            var enumerationVersion = _changeVersion;
            var next = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var inputCount = 0;
            using (var enumerator = ids.GetEnumerator())
            {
                while (true)
                {
                    RequireStableKnownCount(ids, knownCount);
                    if (!enumerator.MoveNext()) break;
                    RequireStableKnownCount(ids, knownCount);

                    if (knownCount.HasValue && inputCount >= knownCount.Value)
                        throw new InvalidOperationException(
                            "Semantic selection traversal produced more entries than its known Count of " + knownCount.Value + ".");
                    if (inputCount >= MaxInputCount)
                        throw new InvalidOperationException("Semantic selection cannot exceed " + MaxInputCount + " input entries.");
                    var raw = enumerator.Current;
                    RequireStableKnownCount(ids, knownCount);
                    inputCount++;
                    if (string.IsNullOrWhiteSpace(raw)) continue;
                    next.Add(raw.Trim());
                }
            }

            if (_changeVersion != enumerationVersion)
                throw new InvalidOperationException("Selection changed while replacement element ids were being enumerated. Retry replacement against the current selection state.");

            var finalKnownCount = ResolveKnownCount(ids);
            if (knownCount.HasValue != finalKnownCount.HasValue ||
                (knownCount.HasValue && knownCount.Value != finalKnownCount!.Value))
                throw new InvalidOperationException(
                    "Semantic selection known Count changed during traversal from " +
                    (knownCount.HasValue ? knownCount.Value.ToString() : "<none>") + " to " +
                    (finalKnownCount.HasValue ? finalKnownCount.Value.ToString() : "<none>") + ".");

            if (knownCount.HasValue && inputCount != knownCount.Value)
                throw new InvalidOperationException(
                    "Semantic selection known Count reported " + knownCount.Value +
                    " entries but traversal produced " + inputCount + ".");

            if (_ids.SetEquals(next)) return;

            var nextVersion = checked(_changeVersion + 1L);
            _ids.Clear();
            foreach (var id in next) _ids.Add(id);
            _changeVersion = nextVersion;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void Clear()
        {
            if (_ids.Count == 0) return;
            var nextVersion = checked(_changeVersion + 1L);
            _ids.Clear();
            _changeVersion = nextVersion;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private static int? ResolveKnownCount(IEnumerable<string> ids)
        {
            int? knownCount = null;
            if (ids is ICollection<string> collection)
                knownCount = AcceptKnownCount(knownCount, collection.Count);
            if (ids is IReadOnlyCollection<string> readOnlyCollection)
                knownCount = AcceptKnownCount(knownCount, readOnlyCollection.Count);
            if (ids is System.Collections.ICollection nonGenericCollection)
                knownCount = AcceptKnownCount(knownCount, nonGenericCollection.Count);
            return knownCount;
        }

        private static void RequireStableKnownCount(IEnumerable<string> ids, int? expectedCount)
        {
            if (!expectedCount.HasValue) return;
            var observedCount = ResolveKnownCount(ids);
            if (!observedCount.HasValue || observedCount.Value != expectedCount.Value)
                throw new InvalidOperationException(
                    "Semantic selection known Count changed during traversal from " + expectedCount.Value + " to " +
                    (observedCount.HasValue ? observedCount.Value.ToString() : "<none>") + ".");
        }

        private static int AcceptKnownCount(int? knownCount, int candidate)
        {
            if (candidate < 0)
                throw new InvalidOperationException("Semantic selection known Count cannot be negative.");
            if (candidate > MaxInputCount)
                throw new InvalidOperationException("Semantic selection cannot exceed " + MaxInputCount + " input entries.");
            if (knownCount.HasValue && knownCount.Value != candidate)
                throw new InvalidOperationException(
                    "Semantic selection exposes conflicting known Counts: " + knownCount.Value + " and " + candidate + ".");
            return candidate;
        }
    }
}
