using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;
using CoreOwnershipPolicy = QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class GeneratedCurtainFrameOwnershipGuard
    {
        private const string HandlesKey = "GeneratedCurtainFrameHandles";

        internal sealed class OwnershipIndex
        {
            private readonly Dictionary<string, string> _owners;
            internal OwnershipIndex(Dictionary<string, string> owners) { _owners = owners; }

            public void EnsureOwned(string handle, ProjectElement element)
            {
                var normalized = (handle ?? string.Empty).Trim();
                if (normalized.Length == 0) throw new InvalidOperationException("Generated curtain frame handle is empty.");
                var expected = element.Id + "/" + HandlesKey;
                if (!_owners.TryGetValue(normalized, out var actual))
                    throw new InvalidOperationException("Generated curtain frame handle " + normalized + " is not owned by project metadata. Refusing destructive erase.");
                if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Generated curtain frame handle " + normalized + " belongs to " + actual + ", not " + expected + ". Refusing destructive erase.");
            }
        }

        public static OwnershipIndex Build(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var owners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                foreach (var handle in element.SourceHandles)
                    Reserve(owners, handle, element.Id + "/SourceHandles");
                foreach (var property in element.Properties)
                {
                    if (!CoreOwnershipPolicy.IsOwnerSlot(property.Key) || string.Equals(property.Key, HandlesKey, StringComparison.OrdinalIgnoreCase)) continue;
                    ReserveProperty(owners, element, property.Key);
                }
            }
            foreach (var element in project.Elements) ReserveProperty(owners, element, HandlesKey);
            return new OwnershipIndex(owners);
        }

        private static void ReserveProperty(Dictionary<string, string> owners, ProjectElement element, string key)
        {
            if (!element.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            foreach (var handle in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
                Reserve(owners, handle, element.Id + "/" + CoreOwnershipPolicy.CanonicalOwnerSlot(key));
        }

        private static void Reserve(Dictionary<string, string> owners, string? handle, string token)
        {
            var normalized = (handle ?? string.Empty).Trim();
            if (normalized.Length == 0) return;
            if (owners.TryGetValue(normalized, out var existing) && !string.Equals(existing, token, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("CAD handle ownership conflict: " + normalized + " is claimed by both " + existing + " and " + token + ".");
            owners[normalized] = token;
        }
    }
}
