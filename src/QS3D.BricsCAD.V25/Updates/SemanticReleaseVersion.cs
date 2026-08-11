using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace QS3D.BricsCAD.V25.Updates
{
    internal sealed class SemanticReleaseVersion : IComparable<SemanticReleaseVersion>
    {
        private static readonly Regex Pattern = new Regex(
            @"^v?(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly string[] _prerelease;

        private SemanticReleaseVersion(int major, int minor, int patch, string[] prerelease, string original)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            _prerelease = prerelease ?? Array.Empty<string>();
            Original = original;
        }

        internal int Major { get; }
        internal int Minor { get; }
        internal int Patch { get; }
        internal bool IsPrerelease => _prerelease.Length != 0;
        internal string Original { get; }

        internal static bool TryParse(string value, out SemanticReleaseVersion version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(value)) return false;

            var trimmed = value.Trim();
            var match = Pattern.Match(trimmed);
            if (!match.Success) return false;

            if (!int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var major) ||
                !int.TryParse(match.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var minor) ||
                !int.TryParse(match.Groups[3].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var patch))
                return false;

            var prerelease = match.Groups[4].Success
                ? match.Groups[4].Value.Split('.')
                : Array.Empty<string>();

            foreach (var identifier in prerelease)
            {
                if (identifier.Length == 0) return false;
                if (IsNumeric(identifier) && identifier.Length > 1 && identifier[0] == '0') return false;
            }

            version = new SemanticReleaseVersion(major, minor, patch, prerelease, trimmed);
            return true;
        }

        internal static SemanticReleaseVersion FromRunningVersion(string informationalVersion, Version assemblyVersion)
        {
            if (TryParse(informationalVersion, out var semantic)) return semantic;

            var fallback = assemblyVersion ?? new Version(0, 0, 0, 0);
            var text = string.Format(
                CultureInfo.InvariantCulture,
                "{0}.{1}.{2}",
                Math.Max(0, fallback.Major),
                Math.Max(0, fallback.Minor),
                Math.Max(0, fallback.Build));
            TryParse(text, out semantic);
            return semantic;
        }

        public int CompareTo(SemanticReleaseVersion other)
        {
            if (ReferenceEquals(other, null)) return 1;

            var core = Major.CompareTo(other.Major);
            if (core != 0) return core;
            core = Minor.CompareTo(other.Minor);
            if (core != 0) return core;
            core = Patch.CompareTo(other.Patch);
            if (core != 0) return core;

            if (_prerelease.Length == 0 && other._prerelease.Length == 0) return 0;
            if (_prerelease.Length == 0) return 1;
            if (other._prerelease.Length == 0) return -1;

            var count = Math.Min(_prerelease.Length, other._prerelease.Length);
            for (var i = 0; i < count; i++)
            {
                var left = _prerelease[i];
                var right = other._prerelease[i];
                var leftNumeric = IsNumeric(left);
                var rightNumeric = IsNumeric(right);

                if (leftNumeric && rightNumeric)
                {
                    var numeric = CompareNumericIdentifiers(left, right);
                    if (numeric != 0) return numeric;
                    continue;
                }

                if (leftNumeric != rightNumeric) return leftNumeric ? -1 : 1;

                var lexical = string.CompareOrdinal(left, right);
                if (lexical != 0) return lexical;
            }

            return _prerelease.Length.CompareTo(other._prerelease.Length);
        }

        public override string ToString() => Original;

        private static bool IsNumeric(string value)
        {
            return value.All(character => character >= '0' && character <= '9');
        }

        private static int CompareNumericIdentifiers(string left, string right)
        {
            var leftTrimmed = left.TrimStart('0');
            var rightTrimmed = right.TrimStart('0');
            if (leftTrimmed.Length == 0) leftTrimmed = "0";
            if (rightTrimmed.Length == 0) rightTrimmed = "0";

            var length = leftTrimmed.Length.CompareTo(rightTrimmed.Length);
            return length != 0 ? length : string.CompareOrdinal(leftTrimmed, rightTrimmed);
        }
    }
}