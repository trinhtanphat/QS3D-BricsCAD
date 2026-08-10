using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class GeneratedRebarOwnershipGuard
    {
        internal sealed class OwnershipIndex
        {
            private readonly Dictionary<string, string> _owners;
            internal OwnershipIndex(Dictionary<string, string> owners) { _owners = owners; }

            public void EnsureOwned(string handle, ProjectElement element, string propertyKey)
            {
                var normalized = (handle ?? string.Empty).Trim();
                if (normalized.Length == 0) throw new InvalidOperationException("Generated rebar handle is empty.");
                var expected = OwnerToken(element, propertyKey);
                if (!_owners.TryGetValue(normalized, out var actual))
                    throw new InvalidOperationException("Generated rebar handle " + normalized + " is not owned by project metadata. Refusing destructive erase.");
                if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Generated rebar handle " + normalized + " belongs to " + actual + ", not " + expected + ". Refusing destructive erase.");
            }
        }

        public static OwnershipIndex Build(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var owners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var element in project.Elements)
            {
                foreach (var handle in element.SourceHandles)
                    AddProtected(handle, element.Id + "/SourceHandles", owners);
                AddProtectedProperty(element, "GeneratedSolidHandle", owners);
                AddProtectedProperty(element, "PhysicalOpeningCutSolidHandle", owners);
                AddProtectedProperty(element, "GeneratedCurtainFrameHandles", owners);
            }

            foreach (var element in project.Elements)
            {
                Add(element, "GeneratedRebarHandles", owners);
                Add(element, "GeneratedShapeRebarHandles", owners);
                Add(element, "GeneratedTieRebarHandles", owners);
                Add(element, "GeneratedBeamStirrupHandles", owners);
                Add(element, "GeneratedSlabMeshHandles", owners);
                Add(element, "GeneratedWallMeshHandles", owners);
            }
            return new OwnershipIndex(owners);
        }

        private static void Add(ProjectElement element, string propertyKey, Dictionary<string, string> owners)
        {
            if (!element.Properties.TryGetValue(propertyKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            var local = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var handle in SplitHandles(raw))
            {
                if (!local.Add(handle)) continue;
                var token = OwnerToken(element, propertyKey);
                if (owners.TryGetValue(handle, out var existing) && !string.Equals(existing, token, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Generated rebar handle ownership conflict: " + handle + " is claimed by both " + existing + " and " + token + ". Refusing destructive erase.");
                owners[handle] = token;
            }
        }

        private static void AddProtectedProperty(ProjectElement element, string propertyKey, Dictionary<string, string> owners)
        {
            if (!element.Properties.TryGetValue(propertyKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            foreach (var handle in SplitHandles(raw)) AddProtected(handle, element.Id + "/" + propertyKey, owners);
        }

        private static void AddProtected(string? handle, string token, Dictionary<string, string> owners)
        {
            var normalized = (handle ?? string.Empty).Trim();
            if (normalized.Length == 0) return;
            if (owners.TryGetValue(normalized, out var existing) && !string.Equals(existing, token, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("CAD handle ownership conflict: " + normalized + " is claimed by both " + existing + " and " + token + ". Refusing destructive erase.");
            owners[normalized] = token;
        }

        private static IEnumerable<string> SplitHandles(string raw) =>
            (raw ?? string.Empty)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase);

        private static string OwnerToken(ProjectElement element, string propertyKey) => element.Id + "/" + propertyKey;
    }
}
