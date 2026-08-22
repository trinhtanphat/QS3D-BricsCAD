using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;
using CoreOwnershipPolicy = QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy;

namespace QS3D.BricsCAD.V25.Cad
{
    internal sealed class GeneratedGeometryInvalidation
    {
        private readonly IList<ProjectElement> _elements;

        internal GeneratedGeometryInvalidation(IList<ProjectElement> elements)
        {
            _elements = elements ?? throw new ArgumentNullException(nameof(elements));
        }

        public int ElementCount => _elements.Count;

        public void CommitMetadata()
        {
            foreach (var element in _elements)
            {
                RemoveByPrefix(element, "GeneratedSolid");
                RemoveByPrefix(element, "PhysicalOpeningCut");
                foreach (var key in CoreOwnershipPolicy.RebarHandleKeys)
                    RemoveByPrefix(element, MetadataPrefixForHandleKey(key));
                RemoveByPrefix(element, "GeneratedCurtainFrame");
                element.ClearGeneratedGeometryStale();
            }
        }

        private static string MetadataPrefixForHandleKey(string key)
        {
            if (key.EndsWith("Handles", StringComparison.OrdinalIgnoreCase)) return key.Substring(0, key.Length - "Handles".Length);
            if (key.EndsWith("Handle", StringComparison.OrdinalIgnoreCase)) return key.Substring(0, key.Length - "Handle".Length);
            return key;
        }

        private static void RemoveByPrefix(ProjectElement element, string prefix)
        {
            var keys = element.Properties.Keys
                .Where(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            foreach (var key in keys) element.Properties.Remove(key);
        }
    }

    internal static class GeneratedDependentGeometryInvalidator
    {
        public static GeneratedGeometryInvalidation Prepare(
            Document document,
            Transaction transaction,
            ProjectState project,
            IEnumerable<ProjectElement> elements)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elements == null) throw new ArgumentNullException(nameof(elements));

            var targets = elements
                .Where(x => x != null)
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToList();
            if (targets.Count == 0) return new GeneratedGeometryInvalidation(targets);

            var needsRebarOwnership = targets.Any(HasGeneratedRebar);
            var rebarOwnership = needsRebarOwnership ? GeneratedRebarOwnershipGuard.Build(project) : null;
            var needsCurtainOwnership = targets.Any(HasCurtainFrames);
            var curtainOwnership = needsCurtainOwnership ? GeneratedCurtainFrameOwnershipGuard.Build(project) : null;
            foreach (var element in targets)
            {
                GeneratedGeometryService.PrepareReplacement(document, transaction, project, element);
                if (rebarOwnership != null)
                    foreach (var key in CoreOwnershipPolicy.RebarHandleKeys)
                        EraseRebarSet(document, transaction, element, key, rebarOwnership);
                if (curtainOwnership != null) EraseCurtainFrames(document, transaction, element, curtainOwnership);
            }
            return new GeneratedGeometryInvalidation(targets);
        }

        private static bool HasGeneratedRebar(ProjectElement element)
        {
            foreach (var key in CoreOwnershipPolicy.RebarHandleKeys)
                if (element.Properties.TryGetValue(key, out var raw) && !string.IsNullOrWhiteSpace(raw)) return true;
            return false;
        }

        private static bool HasCurtainFrames(ProjectElement element) =>
            element.Properties.TryGetValue("GeneratedCurtainFrameHandles", out var raw) && !string.IsNullOrWhiteSpace(raw);

        private static void EraseRebarSet(Document document, Transaction transaction, ProjectElement element, string propertyKey, GeneratedRebarOwnershipGuard.OwnershipIndex ownership)
        {
            if (!element.Properties.TryGetValue(propertyKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            foreach (var handle in SplitHandles(raw))
            {
                ownership.EnsureOwned(handle, element, propertyKey);
                EraseSolid(document, transaction, handle, propertyKey);
            }
        }

        private static void EraseCurtainFrames(Document document, Transaction transaction, ProjectElement element, GeneratedCurtainFrameOwnershipGuard.OwnershipIndex ownership)
        {
            if (!element.Properties.TryGetValue("GeneratedCurtainFrameHandles", out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            foreach (var handle in SplitHandles(raw))
            {
                ownership.EnsureOwned(handle, element);
                EraseSolid(document, transaction, handle, "GeneratedCurtainFrameHandles");
            }
        }

        private static void EraseSolid(Document document, Transaction transaction, string handle, string propertyKey)
        {
            var ids = CadHandleService.Resolve(document, new[] { handle });
            if (ids.Count == 0) return;
            if (ids.Count > 1) throw new InvalidOperationException("Generated geometry handle " + handle + " resolves to multiple live CAD objects.");
            var entity = transaction.GetObject(ids[0], OpenMode.ForWrite, false) as Entity;
            if (entity == null || entity.IsErased) return;
            var solid = entity as Solid3d;
            if (solid == null) throw new InvalidOperationException("Generated " + propertyKey + " handle " + handle + " is live but is not a Solid3d. Refusing destructive invalidation.");
            solid.Erase();
        }

        private static IEnumerable<string> SplitHandles(string raw) =>
            (raw ?? string.Empty)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
