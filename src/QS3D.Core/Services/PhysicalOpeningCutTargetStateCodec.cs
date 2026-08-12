using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public static class PhysicalOpeningCutTargetStateCodec
    {
        public const string OpeningIdsKey = "PhysicalOpeningCutOpeningIdsV1";
        private const int MaxOpeningIds = 4096;
        private const int MaxElementIdLength = 128;
        private const int MaxEncodedIdLength = 1024;
        private const int MaxSerializedLength = 4 * 1024 * 1024;
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public static bool TryRead(ProjectElement host, out IReadOnlyList<string> openingIds)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            openingIds = Array.Empty<string>();
            if (!host.Properties.TryGetValue(OpeningIdsKey, out var raw)) return false;
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException("Host " + host.Id + " has empty physical opening target-state.");
            if (raw.Length > MaxSerializedLength)
                throw new InvalidOperationException("Host " + host.Id + " physical opening target-state exceeds the safety limit.");

            var tokens = raw.Split(new[] { ';' }, StringSplitOptions.None);
            if (tokens.Length > MaxOpeningIds)
                throw new InvalidOperationException("Host " + host.Id + " has too many physical opening targets; limit " + MaxOpeningIds + ".");

            var parsed = new List<string>(tokens.Length);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var token in tokens)
            {
                var encoded = token ?? string.Empty;
                if (encoded.Length == 0 || !string.Equals(encoded, encoded.Trim(), StringComparison.Ordinal))
                    throw new InvalidOperationException("Host " + host.Id + " has malformed or non-canonical physical opening target-state.");
                if (encoded.Length > MaxEncodedIdLength)
                    throw new InvalidOperationException("Host " + host.Id + " has an encoded physical opening target id above the safety limit.");

                string id;
                try
                {
                    var bytes = Convert.FromBase64String(encoded);
                    if (!string.Equals(Convert.ToBase64String(bytes), encoded, StringComparison.Ordinal))
                        throw new InvalidOperationException("Host " + host.Id + " has non-canonical Base64 physical opening target-state.");
                    id = StrictUtf8.GetString(bytes);
                }
                catch (Exception ex) when (ex is FormatException || ex is DecoderFallbackException)
                {
                    throw new InvalidOperationException("Host " + host.Id + " has undecodable physical opening target-state.", ex);
                }

                if (id.Length == 0 ||
                    id.Length > MaxElementIdLength ||
                    !string.Equals(id, id.Trim(), StringComparison.Ordinal) ||
                    !seen.Add(id))
                    throw new InvalidOperationException("Host " + host.Id + " physical opening target-state contains an empty, overlong, non-canonical or duplicate id.");
                parsed.Add(id);
            }

            if (parsed.Count == 0)
                throw new InvalidOperationException("Host " + host.Id + " physical opening target-state contains no opening ids.");
            openingIds = parsed.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
            return true;
        }

        public static IReadOnlyList<ProjectElement> Resolve(ProjectState project, ProjectElement host, IEnumerable<string> openingIds)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (host == null) throw new ArgumentNullException(nameof(host));

            var canonicalHost = project.FindElement(host.Id);
            if (canonicalHost == null)
                throw new InvalidOperationException("Physical opening cut host does not belong to the project: " + host.Id + ".");
            if (!ReferenceEquals(canonicalHost, host))
                throw new InvalidOperationException("Physical opening cut host is detached from the current project instance: " + host.Id + ".");

            var ids = Normalize(openingIds);
            if (ids.Count == 0)
                throw new InvalidOperationException("Host " + host.Id + " physical opening target-state cannot be empty.");

            var result = new List<ProjectElement>(ids.Count);
            foreach (var id in ids)
            {
                var opening = project.FindElement(id) ??
                    throw new InvalidOperationException("Physical opening target no longer exists: " + id + ". Rebuild the host 3D geometry before cutting again.");
                if (!IsOpening(opening))
                    throw new InvalidOperationException("Physical opening target is no longer a Door/WallOpening: " + id + ". Rebuild the host 3D geometry.");
                if (!opening.Properties.TryGetValue("HostWallId", out var linkedHostId) ||
                    !string.Equals(linkedHostId?.Trim(), canonicalHost.Id, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Physical opening target " + id + " is no longer linked to host " + canonicalHost.Id + ". Rebuild the host 3D geometry.");
                result.Add(opening);
            }
            return result.AsReadOnly();
        }

        public static void Write(ProjectElement host, IEnumerable<string> openingIds)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            var ids = Normalize(openingIds);
            if (ids.Count == 0)
                throw new InvalidOperationException("Cannot write empty physical opening target-state for host " + host.Id + ".");

            var serialized = string.Join(";", ids.Select(x => Convert.ToBase64String(StrictUtf8.GetBytes(x))));
            if (serialized.Length > MaxSerializedLength)
                throw new InvalidOperationException("Physical opening target-state exceeds the serialized safety limit for host " + host.Id + ".");
            host.Properties[OpeningIdsKey] = serialized;
        }

        public static IReadOnlyList<string> Normalize(IEnumerable<string> openingIds)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in openingIds ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(raw))
                    throw new InvalidOperationException("Physical opening target-state contains an empty opening id.");
                var id = raw.Trim();
                if (id.Length > MaxElementIdLength)
                    throw new InvalidOperationException("Physical opening target id exceeds " + MaxElementIdLength + " characters.");
                if (!result.Add(id))
                    throw new InvalidOperationException("Physical opening target-state contains duplicate opening id: " + id + ".");
                if (result.Count > MaxOpeningIds)
                    throw new InvalidOperationException("Physical opening target-state exceeds the " + MaxOpeningIds + " opening id limit.");
            }
            return result.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        }

        private static bool IsOpening(ProjectElement element) =>
            element.Category == ElementCategory.Door || element.Category == ElementCategory.WallOpening;
    }
}
