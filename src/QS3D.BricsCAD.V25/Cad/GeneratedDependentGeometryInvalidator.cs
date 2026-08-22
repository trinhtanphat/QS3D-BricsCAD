using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;

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
                Remove(element, "GeneratedSolidHandle");
                Remove(element, "GeneratedSolidCategory");
                Remove(element, "PhysicalOpeningCutSolidHandle");
                Remove(element, "PhysicalOpeningCutFingerprint");
                Remove(element, "PhysicalOpeningCutCount");

                Remove(element, "GeneratedRebarHandles");
                Remove(element, "GeneratedRebarCount");
                Remove(element, "GeneratedRebarDiameterMm");
                Remove(element, "GeneratedRebarCoverM");
                Remove(element, "GeneratedRebarMode");

                Remove(element, "GeneratedShapeRebarHandles");
                Remove(element, "GeneratedShapeRebarCount");
                Remove(element, "GeneratedShapeRebarMode");
            }
        }

        private static void Remove(ProjectElement element, string key)
        {
            if (element.Properties.ContainsKey(key)) element.Properties.Remove(key);
        }
    }

    internal static class GeneratedDependentGeometryInvalidator
    {
        private static readonly string[] RebarHandleKeys =
        {
            "GeneratedRebarHandles",
            "GeneratedShapeRebarHandles"
        };

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
            var ownership = needsRebarOwnership ? GeneratedRebarOwnershipGuard.Build(project) : null;
            foreach (var element in targets)
            {
                GeneratedGeometryService.PrepareReplacement(document, transaction, element);
                if (ownership == null) continue;
                foreach (var key in RebarHandleKeys) EraseRebarSet(document, transaction, element, key, ownership);
            }
            return new GeneratedGeometryInvalidation(targets);
        }

        private static bool HasGeneratedRebar(ProjectElement element)
        {
            foreach (var key in RebarHandleKeys)
                if (element.Properties.TryGetValue(key, out var raw) && !string.IsNullOrWhiteSpace(raw)) return true;
            return false;
        }

        private static void EraseRebarSet(
            Document document,
            Transaction transaction,
            ProjectElement element,
            string propertyKey,
            GeneratedRebarOwnershipGuard.OwnershipIndex ownership)
        {
            if (!element.Properties.TryGetValue(propertyKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            foreach (var handle in SplitHandles(raw))
            {
                ownership.EnsureOwned(handle, element, propertyKey);
                var ids = CadHandleService.Resolve(document, new[] { handle });
                if (ids.Count == 0) continue;
                if (ids.Count > 1) throw new InvalidOperationException("Generated geometry handle " + handle + " resolves to multiple live CAD objects.");
                var entity = transaction.GetObject(ids[0], OpenMode.ForWrite, false) as Entity;
                if (entity == null || entity.IsErased) continue;
                var solid = entity as Solid3d;
                if (solid == null)
                    throw new InvalidOperationException("Generated " + propertyKey + " handle " + handle + " is live but is not a Solid3d. Refusing destructive invalidation.");
                solid.Erase();
            }
        }

        private static IEnumerable<string> SplitHandles(string raw) =>
            (raw ?? string.Empty)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
