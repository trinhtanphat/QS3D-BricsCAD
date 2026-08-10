using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class PhysicalOpeningCutTargetState
    {
        public const string OpeningIdsKey = "PhysicalOpeningCutOpeningIdsV1";

        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public static bool TryRead(ProjectElement host, out IReadOnlyList<string> openingIds)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            openingIds = Array.Empty<string>();
            if (!host.Properties.TryGetValue(OpeningIdsKey, out var raw)) return false;
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException("Host " + host.Id + " có physical opening target-state rỗng.");

            var parsed = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var token in raw.Split(new[] { ';' }, StringSplitOptions.None))
            {
                var encoded = (token ?? string.Empty).Trim();
                if (encoded.Length == 0)
                    throw new InvalidOperationException("Host " + host.Id + " có physical opening target-state malformed.");

                string id;
                try
                {
                    id = StrictUtf8.GetString(Convert.FromBase64String(encoded)).Trim();
                }
                catch (Exception ex) when (ex is FormatException || ex is DecoderFallbackException)
                {
                    throw new InvalidOperationException("Host " + host.Id + " có physical opening target-state không giải mã được.", ex);
                }

                if (id.Length == 0 || !seen.Add(id))
                    throw new InvalidOperationException("Host " + host.Id + " có physical opening target-state rỗng hoặc trùng id.");
                parsed.Add(id);
            }

            if (parsed.Count == 0)
                throw new InvalidOperationException("Host " + host.Id + " có physical opening target-state không chứa opening nào.");
            openingIds = parsed.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
            return true;
        }

        public static IReadOnlyList<ProjectElement> Resolve(ProjectState project, ProjectElement host, IEnumerable<string> openingIds)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (host == null) throw new ArgumentNullException(nameof(host));
            var ids = Normalize(openingIds);
            if (ids.Count == 0)
                throw new InvalidOperationException("Host " + host.Id + " physical opening target-state không được rỗng.");

            var result = new List<ProjectElement>(ids.Count);
            foreach (var id in ids)
            {
                var opening = project.FindElement(id) ??
                    throw new InvalidOperationException("Physical opening target không còn tồn tại: " + id + ". Hãy Build 3D lại host trước khi khoét tiếp.");
                if (!IsOpening(opening))
                    throw new InvalidOperationException("Physical opening target không còn là Door/WallOpening: " + id + ". Hãy Build 3D lại host.");
                if (!opening.Properties.TryGetValue("HostWallId", out var linkedHostId) ||
                    !string.Equals(linkedHostId?.Trim(), host.Id, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Physical opening target " + id + " không còn linked tới host " + host.Id + ". Hãy Build 3D lại host.");
                result.Add(opening);
            }
            return result.AsReadOnly();
        }

        public static void Write(ProjectElement host, IEnumerable<string> openingIds)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            var ids = Normalize(openingIds);
            if (ids.Count == 0)
                throw new InvalidOperationException("Không thể ghi physical opening target-state rỗng cho host " + host.Id + ".");
            host.Properties[OpeningIdsKey] = string.Join(";", ids.Select(x => Convert.ToBase64String(StrictUtf8.GetBytes(x))));
        }

        public static IReadOnlyList<string> Normalize(IEnumerable<string> openingIds)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in openingIds ?? Array.Empty<string>())
            {
                var id = (raw ?? string.Empty).Trim();
                if (id.Length > 0) result.Add(id);
            }
            return result.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        }

        private static bool IsOpening(ProjectElement element) =>
            element.Category == ElementCategory.Door || element.Category == ElementCategory.WallOpening;
    }
}
