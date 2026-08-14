using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;
using CoreOwnershipPolicy = QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class GeneratedRebarOwnershipGuard
    {
        internal sealed class OwnershipIndex
        {
            private readonly Dictionary<string, string> _owners;
            private readonly ProjectState _project;
            private readonly Document? _document;
            private readonly HashSet<string> _validatedLiveSets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            internal OwnershipIndex(Dictionary<string, string> owners, ProjectState project, Document? document)
            {
                _owners = owners ?? throw new ArgumentNullException(nameof(owners));
                _project = project ?? throw new ArgumentNullException(nameof(project));
                _document = document;
            }

            public void EnsureOwned(string handle, ProjectElement element, string propertyKey)
            {
                if (element == null) throw new ArgumentNullException(nameof(element));
                if (string.IsNullOrWhiteSpace(propertyKey)) throw new ArgumentException("Generated rebar owner slot is required.", nameof(propertyKey));
                var normalized = CanonicalHandle(handle, "Generated rebar handle");
                var expectedOwner = OwnerToken(element, propertyKey);
                if (!_owners.TryGetValue(normalized, out var actual))
                    throw new InvalidOperationException("Generated rebar handle " + normalized + " is not owned by project metadata. Refusing destructive erase.");
                if (!string.Equals(actual, expectedOwner, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Generated rebar handle " + normalized + " belongs to " + actual + ", not " + expectedOwner + ". Refusing destructive erase.");

                EnsureCompleteLiveSet(element, propertyKey, expectedOwner);
            }

            private void EnsureCompleteLiveSet(ProjectElement element, string propertyKey, string expectedOwner)
            {
                if (_validatedLiveSets.Contains(expectedOwner)) return;
                if (_document == null)
                    throw new InvalidOperationException("No active BricsCAD document is available to validate generated rebar before destructive replacement.");
                if (!ReferenceEquals(_document, Application.DocumentManager.MdiActiveDocument))
                    throw new InvalidOperationException("Active DWG changed before generated rebar replacement. Refusing destructive replacement before any rebar is erased.");
                if (!element.Properties.TryGetValue(propertyKey, out var raw) || string.IsNullOrWhiteSpace(raw))
                    throw new InvalidOperationException("Generated rebar owner metadata disappeared before destructive replacement: " + expectedOwner + ".");

                var expectedHandles = new List<string>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var token in SplitHandles(raw))
                {
                    var canonical = CanonicalHandle(token, expectedOwner + " handle");
                    if (!seen.Add(canonical))
                        throw new InvalidOperationException("Generated rebar metadata contains duplicate canonical handle " + canonical + ". Refusing destructive replacement before any rebar is erased.");
                    if (!_owners.TryGetValue(canonical, out var actual) || !string.Equals(actual, expectedOwner, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Generated rebar handle " + canonical + " is not exclusively owned by " + expectedOwner + ". Refusing destructive replacement before any rebar is erased.");
                    expectedHandles.Add(canonical);
                }

                if (expectedHandles.Count == 0)
                    throw new InvalidOperationException("Generated rebar metadata contains no valid handles for " + expectedOwner + ". Refusing destructive replacement before any rebar is erased.");

                var ids = CadHandleService.Resolve(_document, expectedHandles);
                if (ids.Count != expectedHandles.Count)
                    throw new InvalidOperationException("Generated rebar live-handle set is incomplete for " + expectedOwner + ". Refusing destructive replacement before any rebar is erased.");

                using (var transaction = _document.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    foreach (var id in ids)
                    {
                        var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                        if (!(entity is Solid3d solid) || solid.IsErased)
                            throw new InvalidOperationException("Generated rebar set contains a missing, erased, or non-Solid3d object for " + expectedOwner + ". Refusing destructive replacement before any rebar is erased.");
                        GeneratedRebarNativeOwnershipService.RequireMatchingOwnership(
                            solid,
                            _project,
                            element,
                            propertyKey,
                            "validate generated rebar replacement");
                    }
                    transaction.Commit();
                }

                _validatedLiveSets.Add(expectedOwner);
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
                foreach (var property in element.Properties)
                {
                    if (!CoreOwnershipPolicy.IsOwnerSlot(property.Key) || CoreOwnershipPolicy.IsRebarOwnerSlot(property.Key)) continue;
                    AddProtectedProperty(element, property.Key, owners);
                }
            }

            foreach (var element in project.Elements)
                foreach (var key in CoreOwnershipPolicy.RebarHandleKeys)
                    Add(element, key, owners);

            return new OwnershipIndex(owners, project, Application.DocumentManager.MdiActiveDocument);
        }

        private static void Add(ProjectElement element, string propertyKey, Dictionary<string, string> owners)
        {
            if (!element.Properties.TryGetValue(propertyKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            var local = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var count = 0;
            foreach (var handle in SplitHandles(raw))
            {
                count++;
                var canonical = CanonicalHandle(handle, element.Id + "/" + propertyKey);
                if (!local.Add(canonical)) continue;
                var token = OwnerToken(element, propertyKey);
                if (owners.TryGetValue(canonical, out var existing) && !string.Equals(existing, token, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Generated rebar handle ownership conflict: " + canonical + " is claimed by both " + existing + " and " + token + ". Refusing destructive erase.");
                owners[canonical] = token;
            }
            if (count == 0)
                throw new InvalidOperationException("Generated rebar metadata contains no handles for " + OwnerToken(element, propertyKey) + ". Refusing destructive replacement.");
        }

        private static void AddProtectedProperty(ProjectElement element, string propertyKey, Dictionary<string, string> owners)
        {
            if (!element.Properties.TryGetValue(propertyKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            foreach (var handle in SplitHandles(raw)) AddProtected(handle, element.Id + "/" + propertyKey, owners);
        }

        private static void AddProtected(string? handle, string token, Dictionary<string, string> owners)
        {
            if (string.IsNullOrWhiteSpace(handle)) return;
            var canonical = CanonicalHandle(handle, token);
            if (!owners.ContainsKey(canonical)) owners[canonical] = token;
        }

        private static IEnumerable<string> SplitHandles(string raw) =>
            (raw ?? string.Empty)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0);

        private static string CanonicalHandle(string? handle, string label)
        {
            var canonical = CadHandleService.NormalizeHexHandle(handle);
            if (canonical == null)
                throw new InvalidOperationException(label + " is not a valid CAD handle: " + (handle ?? "<null>") + ". Refusing destructive replacement.");
            return canonical;
        }

        private static string OwnerToken(ProjectElement element, string propertyKey) =>
            element.Id + "/" + CoreOwnershipPolicy.CanonicalOwnerSlot(propertyKey);
    }
}
