using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QS3D.Core.Features
{
    public sealed class FeatureFlags
    {
        private readonly Dictionary<string, bool> _flags = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public bool IsEnabled(string name)
        {
            if (!TryNormalizeName(name, out var normalized)) return false;
            return _flags.TryGetValue(normalized, out var value) && value;
        }

        public void Set(string name, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Feature name is required.", nameof(name));
            if (!TryNormalizeName(name, out var normalized))
                throw new ArgumentException("Feature name cannot contain control characters.", nameof(name));
            _flags[normalized] = enabled;
        }

        public IReadOnlyDictionary<string, bool> Snapshot() =>
            new ReadOnlyDictionary<string, bool>(new Dictionary<string, bool>(_flags, StringComparer.OrdinalIgnoreCase));

        private static bool TryNormalizeName(string name, out string normalized)
        {
            normalized = null;
            if (string.IsNullOrWhiteSpace(name)) return false;

            var candidate = name.Trim();
            for (var index = 0; index < candidate.Length; index++)
            {
                if (char.IsControl(candidate[index])) return false;
            }

            normalized = candidate;
            return true;
        }
    }
}
