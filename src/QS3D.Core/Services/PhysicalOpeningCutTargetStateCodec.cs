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
            if (string.IsNullOrWhiteSpace(raw)) throw new InvalidOperationException("Host " + host.Id + " has empty physical opening target-state.");
            if (raw.Length > MaxSerializedLength) throw new InvalidOperationException("Host " + host.Id + " physical opening target-state exceeds the safety limit.");
            var tokens = raw.Split(new[] { ';' }, MaxOpeningIds + 1, StringSplitOptions.None);
            if (tokens.Length > MaxOpeningIds) throw new InvalidOperationException("Host " + host.Id + " has too many physical opening targets; limit " + MaxOpeningIds + ".");
            var parsed = new List<string>(tokens.Length);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var token in tokens)
            {
                var encoded = token ?? string.Empty;
                if (encoded.Length == 0 || !string.Equals(encoded, encoded.Trim(), StringComparison.Ordinal)) throw new InvalidOperationException("Host " + host.Id + " has malformed or non-canonical physical opening target-state.");
                if (encoded.Length > MaxEncodedIdLength) throw new InvalidOperationException("Host " + host.Id + " has an encoded physical opening target id above the safety limit.");
                string id;
                try
                {
                    var bytes = Convert.FromBase64String(encoded);
                    if (!string.Equals(Convert.ToBase64String(bytes), encoded, StringComparison.Ordinal)) throw new InvalidOperationException("Host " + host.Id + " has non-canonical Base64 physical opening target-state.");
                    id = StrictUtf8.GetString(bytes);
                }
                catch (Exception ex) when (ex is FormatException || ex is DecoderFallbackException)
                {
                    throw new InvalidOperationException("Host " + host.Id + " has undecodable physical opening target-state.", ex);
                }
                if (id.Length == 0 || id.Length > MaxElementIdLength || !string.Equals(id, id.Trim(), StringComparison.Ordinal) || !seen.Add(id))
                    throw new InvalidOperationException("Host " + host.Id + " physical opening target-state contains an empty, overlong, non-canonical or duplicate id.");
                parsed.Add(id);
            }
            if (parsed.Count == 0) throw new InvalidOperationException("Host " + host.Id + " physical opening target-state contains no opening ids.");
            var canonical = parsed.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
            if (!parsed.SequenceEqual(canonical, StringComparer.Ordinal)) throw new InvalidOperationException("Host " + host.Id + " physical opening target-state is not in canonical opening-id order.");
            openingIds = canonical.AsReadOnly();
            return true;
        }

        public static IReadOnlyList<ProjectElement> Resolve(ProjectState project, ProjectElement host, IEnumerable<string> openingIds)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (host == null) throw new ArgumentNullException(nameof(host));
            ValidateProjectElements(project);
            var sourceElements = project.Elements.ToArray();
            var sourceIndex = sourceElements.ToDictionary(x => x.Id, x => x, StringComparer.OrdinalIgnoreCase);
            var canonicalHost = project.FindElement(host.Id);
            if (canonicalHost == null) throw new InvalidOperationException("Physical opening cut host does not belong to the project: " + host.Id + ".");
            if (!ReferenceEquals(canonicalHost, host)) throw new InvalidOperationException("Physical opening cut host is detached from the current project instance: " + host.Id + ".");
            var targetEnumerationVersion = project.ChangeVersion;
            var ids = Normalize(openingIds);
            if (project.ChangeVersion != targetEnumerationVersion) throw new InvalidOperationException("Project changed while physical opening target ids were being enumerated; recompute the target set against the current project state.");
            if (ids.Count == 0) throw new InvalidOperationException("Host " + host.Id + " physical opening target-state cannot be empty.");
            ValidateProjectElements(project);
            RequireElementStructureFresh(project, sourceElements);
            var currentHost = project.FindElement(canonicalHost.Id);
            if (!ReferenceEquals(currentHost, canonicalHost)) throw new InvalidOperationException("Physical opening cut host no longer belongs to the project after opening target enumeration: " + canonicalHost.Id + ".");
            var result = new List<ProjectElement>(ids.Count);
            foreach (var id in ids)
            {
                if (!sourceIndex.TryGetValue(id, out var opening)) throw new InvalidOperationException("Physical opening target no longer exists: " + id + ". Rebuild the host 3D geometry before cutting again.");
                if (!IsOpening(opening)) throw new InvalidOperationException("Physical opening target is no longer a Door/WallOpening: " + id + ". Rebuild the host 3D geometry.");
                if (!opening.Properties.TryGetValue("HostWallId", out var linkedHostId) || string.IsNullOrWhiteSpace(linkedHostId)) throw new InvalidOperationException("Physical opening target " + id + " is no longer linked to host " + canonicalHost.Id + ". Rebuild the host 3D geometry.");
                if (!string.Equals(linkedHostId, linkedHostId.Trim(), StringComparison.Ordinal)) throw new InvalidOperationException("Physical opening target " + id + " has a non-canonical HostWallId relation. Repair semantic relations before trusting physical cut ownership.");
                if (!string.Equals(linkedHostId, canonicalHost.Id, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Physical opening target " + id + " is no longer linked to host " + canonicalHost.Id + ". Rebuild the host 3D geometry.");
                result.Add(opening);
            }
            if (project.ChangeVersion != targetEnumerationVersion) throw new InvalidOperationException("Project changed while physical opening targets were being resolved; recompute the target set against the current project state.");
            RequireElementStructureFresh(project, sourceElements);
            return result.AsReadOnly();
        }

        public static void Write(ProjectElement host, IEnumerable<string> openingIds)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            var ids = Normalize(openingIds);
            if (ids.Count == 0) throw new InvalidOperationException("Cannot write empty physical opening target-state for host " + host.Id + ".");
            var serialized = string.Join(";", ids.Select(x => Convert.ToBase64String(StrictUtf8.GetBytes(x))));
            if (serialized.Length > MaxSerializedLength) throw new InvalidOperationException("Physical opening target-state exceeds the serialized safety limit for host " + host.Id + ".");
            host.Properties[OpeningIdsKey] = serialized;
        }

        public static IReadOnlyList<string> Normalize(IEnumerable<string> openingIds)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in openingIds ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(raw)) throw new InvalidOperationException("Physical opening target-state contains an empty opening id.");
                if (!string.Equals(raw, raw.Trim(), StringComparison.Ordinal)) throw new InvalidOperationException("Physical opening target-state contains a non-canonical opening id with leading or trailing whitespace.");
                var id = raw;
                if (id.Length > MaxElementIdLength) throw new InvalidOperationException("Physical opening target id exceeds " + MaxElementIdLength + " characters.");
                if (!result.Add(id)) throw new InvalidOperationException("Physical opening target-state contains duplicate opening id: " + id + ".");
                if (result.Count > MaxOpeningIds) throw new InvalidOperationException("Physical opening target-state exceeds the " + MaxOpeningIds + " opening id limit.");
            }
            return result.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        }

        private static void ValidateProjectElements(ProjectState project)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null) throw new InvalidOperationException("Project contains a null semantic element entry.");
                var id = element.Id ?? string.Empty;
                if (string.IsNullOrWhiteSpace(id)) throw new InvalidOperationException("Project contains an element with a blank semantic id.");
                if (!string.Equals(id, id.Trim(), StringComparison.Ordinal)) throw new InvalidOperationException("Project contains an element with a non-canonical semantic id: " + id + ".");
                if (!seen.Add(id)) throw new InvalidOperationException("Project contains duplicate semantic element id: " + id + ".");
            }
        }

        private static void RequireElementStructureFresh(ProjectState project, IReadOnlyList<ProjectElement> sourceElements)
        {
            if (project.Elements.Count != sourceElements.Count) throw StructuralFreshnessError();
            for (var index = 0; index < sourceElements.Count; index++) if (!ReferenceEquals(project.Elements[index], sourceElements[index])) throw StructuralFreshnessError();
        }

        private static InvalidOperationException StructuralFreshnessError() => new InvalidOperationException("Project element structure changed while physical opening target ids were being enumerated; recompute the target set against the current project state.");
        private static bool IsOpening(ProjectElement element) => element.Category == ElementCategory.Door || element.Category == ElementCategory.WallOpening;
    }
}
