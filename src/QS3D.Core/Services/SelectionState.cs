using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Services
{
    public sealed class SelectionState
    {
        private readonly HashSet<string> _ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public event EventHandler? Changed;
        public IReadOnlyCollection<string> ElementIds => _ids.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();

        public void Replace(IEnumerable<string> ids)
        {
            if (ids == null) throw new ArgumentNullException(nameof(ids));
            var next = new HashSet<string>(ids
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);
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
