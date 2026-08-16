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
            var normalized = NormalizeName(name);
            _flags[normalized] = enabled;
        }

        public IReadOnlyDictionary<string, bool> Snapshot() =>
            new ReadOnlyDictionary<string, bool>(new Dictionary<string, bool>(_flags, StringComparer.OrdinalIgnoreCase));

        private static string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Feature name is required.", nameof(name));

            var normalized = name.Trim();
            if (ContainsControlCharacter(normalized))
                throw new ArgumentException("Feature name cannot contain control characters.", nameof(name));
            return normalized;
        }

        private static bool TryNormalizeName(string name, out string normalized)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                normalized = string.Empty;
                return false;
            }

            normalized = name.Trim();
            if (ContainsControlCharacter(normalized))
            {
                normalized = string.Empty;
                return false;
            }
            return true;
        }

        private static bool ContainsControlCharacter(string value)
        {
            for (var index = 0; index < value.Length; index++)
                if (char.IsControl(value[index])) return true;
            return false;
        }
    }
}
