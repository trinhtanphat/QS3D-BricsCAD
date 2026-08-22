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
                Remove(element, "PhysicalOpeningCutMode");

                Remove(element, "GeneratedRebarHandles");
                Remove(element, "GeneratedRebarCount");
                Remove(element, "GeneratedRebarDiameterMm");
                Remove(element, "GeneratedRebarCoverM");
                Remove(element, "GeneratedRebarMode");
                Remove(element, "GeneratedRebarBeamEndCoverM");
                Remove(element, "GeneratedRebarBeamTopCount");
                Remove(element, "GeneratedRebarBeamBottomCount");

                Remove(element, "GeneratedShapeRebarHandles");
                Remove(element, "GeneratedShapeRebarCount");
                Remove(element, "GeneratedShapeRebarMode");

                Remove(element, "GeneratedTieRebarHandles");
                Remove(element, "GeneratedTieRebarCount");
                Remove(element, "GeneratedTieRebarDiameterMm");
                Remove(element, "GeneratedTieRebarActualSpacingM");
                Remove(element, "GeneratedTieRebarCoverM");
                Remove(element, "GeneratedTieRebarMode");

                Remove(element, "GeneratedBeamStirrupHandles");
                Remove(element, "GeneratedBeamStirrupCount");
                Remove(element, "GeneratedBeamStirrupDiameterMm");
                Remove(element, "GeneratedBeamStirrupActualSpacingM");
                Remove(element, "GeneratedBeamStirrupNotation");
                Remove(element, "GeneratedBeamStirrupMode");

                Remove(element, "GeneratedSlabMeshHandles");
                Remove(element, "GeneratedSlabMeshCount");
                Remove(element, "GeneratedSlabMeshXDiameterMm");
                Remove(element, "GeneratedSlabMeshYDiameterMm");
                Remove(element, "GeneratedSlabMeshCoverM");
                Remove(element, "GeneratedSlabMeshMode");
                Remove(element, "GeneratedSlabMeshXActualSpacingM");
                Remove(element, "GeneratedSlabMeshYActualSpacingM");
                Remove(element, "GeneratedSlabMeshFaces");

                Remove(element, "GeneratedWallMeshHandles");
                Remove(element, "GeneratedWallMeshCount");
                Remove(element, "GeneratedWallMeshHorizontalDiameterMm");
                Remove(element, "GeneratedWallMeshVerticalDiameterMm");
                Remove(element, "GeneratedWallMeshCoverM");
                Remove(element, "GeneratedWallMeshMode");
                Remove(element, "GeneratedWallMeshHorizontalActualSpacingM");
                Remove(element, "GeneratedWallMeshVerticalActualSpacingM");
                Remove(element, "GeneratedWallMeshFaces");

                Remove(element, "GeneratedCurtainFrameHandles");
                Remove(element, "GeneratedCurtainFrameCount");
                Remove(element, "GeneratedCurtainFrameBaseCount");
                Remove(element, "GeneratedCurtainFrameOpeningCount");
                Remove(element, "GeneratedCurtainFrameColumns");
                Remove(element, "GeneratedCurtainFrameRows");
                Remove(element, "GeneratedCurtainFrameDepthM");
                Remove(element, "GeneratedCurtainFrameSourceLengthM");
                Remove(element, "GeneratedCurtainFrameHeightM");
                Remove(element, "GeneratedCurtainFrameConfigFingerprint");
                Remove(element, "GeneratedCurtainFrameMode");
                element.ClearGeneratedGeometryStale();
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
            "GeneratedShapeRebarHandles",
            "GeneratedTieRebarHandles",
            "GeneratedBeamStirrupHandles",
            "GeneratedSlabMeshHandles",
            "GeneratedWallMeshHandles"
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
            var rebarOwnership = needsRebarOwnership ? GeneratedRebarOwnershipGuard.Build(project) : null;
            var needsCurtainOwnership = targets.Any(HasCurtainFrames);
            var curtainOwnership = needsCurtainOwnership ? GeneratedCurtainFrameOwnershipGuard.Build(project) : null;
            foreach (var element in targets)
            {
                GeneratedGeometryService.PrepareReplacement(document, transaction, project, element);
                if (rebarOwnership != null)
                    foreach (var key in RebarHandleKeys) EraseRebarSet(document, transaction, element, key, rebarOwnership);
                if (curtainOwnership != null) EraseCurtainFrames(document, transaction, element, curtainOwnership);
            }
            return new GeneratedGeometryInvalidation(targets);
        }

        private static bool HasGeneratedRebar(ProjectElement element)
        {
            foreach (var key in RebarHandleKeys)
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
