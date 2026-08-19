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
            var knownCount = ResolveKnownCount(ids);

            var enumerationVersion = _changeVersion;
            var next = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var inputCount = 0;
            foreach (var raw in ids)
            {
                if (inputCount >= MaxInputCount)
                    throw new InvalidOperationException("Semantic selection cannot exceed " + MaxInputCount + " input entries.");
                inputCount++;
                if (string.IsNullOrWhiteSpace(raw)) continue;
                next.Add(raw.Trim());
            }

            if (_changeVersion != enumerationVersion)
                throw new InvalidOperationException("Selection changed while replacement element ids were being enumerated. Retry replacement against the current selection state.");
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
            return knownCount;
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
