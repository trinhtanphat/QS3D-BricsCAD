using System;
using System.Collections.Generic;

namespace QS3D.Core.Features
{
    public sealed class FeatureFlags
    {
        private readonly Dictionary<string, bool> _flags = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        public bool IsEnabled(string name) => !string.IsNullOrWhiteSpace(name) && _flags.TryGetValue(name.Trim(), out var value) && value;
        public void Set(string name, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Feature name is required.", nameof(name));
            _flags[name.Trim()] = enabled;
        }
        public IReadOnlyDictionary<string, bool> Snapshot() => new Dictionary<string, bool>(_flags, StringComparer.OrdinalIgnoreCase);
    }
}
