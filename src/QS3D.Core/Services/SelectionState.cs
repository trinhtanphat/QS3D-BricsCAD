using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Services
{
    public sealed class SelectionState
    {
        private const int MaxInputCount = 10000;
        private readonly HashSet<string> _ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public event EventHandler? Changed;
        public IReadOnlyCollection<string> ElementIds => _ids.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();

        public void Replace(IEnumerable<string> ids)
        {
            if (ids == null) throw new ArgumentNullException(nameof(ids));
            if (ids is ICollection<string> collection && collection.Count > MaxInputCount)
                throw new InvalidOperationException("Semantic selection cannot exceed " + MaxInputCount + " input entries.");
            if (ids is IReadOnlyCollection<string> readOnlyCollection && readOnlyCollection.Count > MaxInputCount)
                throw new InvalidOperationException("Semantic selection cannot exceed " + MaxInputCount + " input entries.");

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

            if (_ids.SetEquals(next)) return;
            _ids.Clear();
            foreach (var id in next) _ids.Add(id);
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void Clear()
        {
            if (_ids.Count == 0) return;
            _ids.Clear();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
