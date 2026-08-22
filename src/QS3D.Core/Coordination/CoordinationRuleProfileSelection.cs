using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.Coordination
{
    /// <summary>
    /// Immutable reference to one exact coordination rule-profile revision.
    /// There is deliberately no "latest" concept: callers must persist both values.
    /// </summary>
    public sealed class CoordinationRuleProfileBinding
    {
        public CoordinationRuleProfileBinding(string profileId, int profileVersion)
        {
            ProfileId = Required(profileId, nameof(profileId));
            if (profileVersion <= 0)
                throw new ArgumentOutOfRangeException(nameof(profileVersion), "Profile version must be positive.");
            ProfileVersion = profileVersion;
        }

        public string ProfileId { get; }
        public int ProfileVersion { get; }

        private static string Required(string value, string parameterName)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0) throw new ArgumentException("Value is required.", parameterName);
            if (normalized.Any(char.IsControl)) throw new ArgumentException("Control characters are not allowed.", parameterName);
            return normalized;
        }
    }

    /// <summary>
    /// Immutable catalog for explicit profile-version selection. Multiple revisions of the
    /// same profile ID may coexist, but every bind/resolve operation addresses one exact revision.
    /// </summary>
    public sealed class CoordinationRuleProfileCatalog
    {
        private readonly ReadOnlyCollection<CoordinationRuleProfile> _profiles;

        public CoordinationRuleProfileCatalog(IEnumerable<CoordinationRuleProfile> profiles)
        {
            if (profiles == null) throw new ArgumentNullException(nameof(profiles));

            var snapshot = profiles.ToArray();
            if (snapshot.Any(profile => profile == null))
                throw new ArgumentException("Rule-profile catalog cannot contain null profiles.", nameof(profiles));

            var duplicate = snapshot
                .GroupBy(
                    profile => new ProfileIdentity(profile.ProfileId, profile.ProfileVersion),
                    ProfileIdentityComparer.Instance)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
            {
                throw new ArgumentException(
                    "Rule-profile catalog contains duplicate profile revision: " +
                    duplicate.Key.ProfileId + " v" + duplicate.Key.ProfileVersion + ".",
                    nameof(profiles));
            }

            _profiles = Array.AsReadOnly(snapshot);
        }

        public IReadOnlyList<CoordinationRuleProfile> Profiles => _profiles;

        public CoordinationRuleProfileBinding Bind(string profileId, int profileVersion)
        {
            var requested = new CoordinationRuleProfileBinding(profileId, profileVersion);
            FindExact(requested);
            return requested;
        }

        public CoordinationRuleResolution? Resolve(
            CoordinationRuleProfileBinding binding,
            string leftCategory,
            string rightCategory)
        {
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            return FindExact(binding).Resolve(leftCategory, rightCategory);
        }

        private CoordinationRuleProfile FindExact(CoordinationRuleProfileBinding binding)
        {
            var matches = _profiles
                .Where(profile =>
                    profile.ProfileVersion == binding.ProfileVersion &&
                    string.Equals(profile.ProfileId, binding.ProfileId, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (matches.Length == 1) return matches[0];

            throw new InvalidOperationException(
                "Coordination rule profile revision is not available: " +
                binding.ProfileId + " v" + binding.ProfileVersion + ".");
        }

        private sealed class ProfileIdentity
        {
            public ProfileIdentity(string profileId, int profileVersion)
            {
                ProfileId = profileId;
                ProfileVersion = profileVersion;
            }

            public string ProfileId { get; }
            public int ProfileVersion { get; }
        }

        private sealed class ProfileIdentityComparer : IEqualityComparer<ProfileIdentity>
        {
            public static readonly ProfileIdentityComparer Instance = new ProfileIdentityComparer();

            public bool Equals(ProfileIdentity x, ProfileIdentity y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x == null || y == null) return false;
                return x.ProfileVersion == y.ProfileVersion &&
                       string.Equals(x.ProfileId, y.ProfileId, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(ProfileIdentity obj)
            {
                if (obj == null) return 0;
                unchecked
                {
                    return (StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ProfileId) * 397) ^ obj.ProfileVersion;
                }
            }
        }
    }
}
