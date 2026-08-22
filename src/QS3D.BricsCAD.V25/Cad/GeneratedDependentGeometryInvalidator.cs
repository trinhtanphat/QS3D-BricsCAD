using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Diagnostics;
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
                RemoveByPrefix(element, "GeneratedCurtainPanel");
                RemoveByPrefix(element, "GeneratedGridAnnotation");
                RemoveByPrefix(element, "GeneratedSemanticTag");
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
        private const string GeneratedSolidHandleKey = "GeneratedSolidHandle";
        private const string CurtainFrameHandlesKey = "GeneratedCurtainFrameHandles";
        private const string CurtainPanelHandlesKey = "GeneratedCurtainPanelHandles";

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
            var needsCurtainPanelOwnership = targets.Any(HasCurtainPanels);
            var curtainPanelOwnership = needsCurtainPanelOwnership ? GeneratedCurtainPanelOwnershipGuard.Build(project) : null;

            EnsureCompleteLiveHandleSets(document, project, targets, rebarOwnership, curtainOwnership, curtainPanelOwnership);

            foreach (var element in targets)
            {
                GeneratedGeometryService.PrepareReplacement(document, transaction, project, element);
                if (rebarOwnership != null)
                    foreach (var key in CoreOwnershipPolicy.RebarHandleKeys)
                        EraseRebarSet(document, transaction, project, element, key, rebarOwnership);
                if (curtainOwnership != null) EraseCurtainFrames(document, transaction, project, element, curtainOwnership);
                if (curtainPanelOwnership != null) EraseCurtainPanels(document, transaction, project, element, curtainPanelOwnership);
                EraseGridAnnotations(document, transaction, project, element);
                EraseSemanticTags(document, transaction, project, element);
            }
            return new GeneratedGeometryInvalidation(targets);
        }

        private static void EnsureCompleteLiveHandleSets(
            Document document,
            ProjectState project,
            IList<ProjectElement> targets,
            GeneratedRebarOwnershipGuard.OwnershipIndex? rebarOwnership,
            GeneratedCurtainFrameOwnershipGuard.OwnershipIndex? curtainOwnership,
            GeneratedCurtainPanelOwnershipGuard.OwnershipIndex? curtainPanelOwnership)
        {
            foreach (var element in targets)
            {
                EnsureGeneratedSolidLive(document, project, element);

                if (rebarOwnership != null)
                {
                    foreach (var key in CoreOwnershipPolicy.RebarHandleKeys)
                    {
                        if (!element.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) continue;
                        var expected = ParseExpectedHandles(raw, element, key);
                        foreach (var handle in expected) rebarOwnership.EnsureOwned(handle, element, key);
                        EnsureRebarSetLive(document, project, element, key, expected);
                    }
                }

                if (curtainOwnership != null &&
                    element.Properties.TryGetValue(CurtainFrameHandlesKey, out var curtainRaw) &&
                    !string.IsNullOrWhiteSpace(curtainRaw))
                {
                    var expected = ParseExpectedHandles(curtainRaw, element, CurtainFrameHandlesKey);
                    foreach (var handle in expected) curtainOwnership.EnsureOwned(handle, element);
                    EnsureCurtainFrameSetLive(document, project, element, expected);
                }

                if (curtainPanelOwnership != null && HasCurtainPanels(element))
                {
                    var expected = ParseCurtainPanelExpectedHandles(element);
                    foreach (var handle in expected) curtainPanelOwnership.EnsureOwned(handle, element);
                    if (expected.Count > 0) EnsureCurtainPanelSetLive(document, project, element, expected);
                }

                EnsureGridAnnotationsLive(document, project, element);
                EnsureSemanticTagsLive(document, project, element);
            }
        }

        private static void EnsureGeneratedSolidLive(Document document, ProjectState project, ProjectElement element)
        {
            if (!element.Properties.TryGetValue(GeneratedSolidHandleKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            var expected = ParseExpectedHandles(raw, element, GeneratedSolidHandleKey);
            if (expected.Count != 1)
                throw new InvalidOperationException(
                    GeneratedSolidHandleKey + " for " + element.Id + " must contain exactly one CAD handle. Refusing destructive invalidation before any generated geometry is erased.");

            var ids = ResolveCompleteSet(document, element, GeneratedSolidHandleKey, expected);
            using (var validation = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var entity = validation.GetObject(ids[0], OpenMode.ForRead, false) as Entity;
                if (entity == null || entity.IsErased)
                    throw new InvalidOperationException(
                        "Generated " + GeneratedSolidHandleKey + " for " + element.Id + " is not a live Entity. Refusing destructive invalidation before any generated geometry is erased.");
                if (!(entity is Solid3d))
                    throw new InvalidOperationException(
                        "Generated " + GeneratedSolidHandleKey + " " + expected[0] + " for " + element.Id + " is live but is not a Solid3d. Refusing destructive invalidation before any generated geometry is erased.");
                GeneratedGeometryService.RequireMatchingOwnership(entity, project, element, "validate generated Solid3d " + expected[0]);
                validation.Commit();
            }
        }

        private static void EnsureRebarSetLive(
            Document document,
            ProjectState project,
            ProjectElement element,
            string propertyKey,
            IReadOnlyList<string> expected)
        {
            var ids = ResolveCompleteSet(document, element, propertyKey, expected);
            using (var validation = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in ids)
                {
                    var entity = validation.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased)
                        throw new InvalidOperationException(
                            "Generated " + propertyKey + " for " + element.Id + " resolved to a non-live Entity. Refusing destructive invalidation before any generated geometry is erased.");
                    var solid = entity as Solid3d;
                    if (solid == null)
                        throw new InvalidOperationException(
                            "Generated " + propertyKey + " handle " + id.Handle + " for " + element.Id + " is live but is not a Solid3d. Refusing destructive invalidation before any generated geometry is erased.");
                    GeneratedRebarNativeOwnershipService.RequireMatchingOwnership(
                        solid,
                        project,
                        element,
                        propertyKey,
                        "validate generated rebar " + id.Handle);
                }
                validation.Commit();
            }
        }

        private static void EnsureCurtainFrameSetLive(
            Document document,
            ProjectState project,
            ProjectElement element,
            IReadOnlyList<string> expected)
        {
            var ids = ResolveCompleteSet(document, element, CurtainFrameHandlesKey, expected);
            using (var validation = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in ids)
                {
                    var entity = validation.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased)
                        throw new InvalidOperationException(
                            "Generated " + CurtainFrameHandlesKey + " for " + element.Id + " resolved to a non-live Entity. Refusing destructive invalidation before any generated geometry is erased.");
                    var solid = entity as Solid3d;
                    if (solid == null)
                        throw new InvalidOperationException(
                            "Generated " + CurtainFrameHandlesKey + " handle " + id.Handle + " for " + element.Id + " is live but is not a Solid3d. Refusing destructive invalidation before any generated geometry is erased.");
                    GeneratedCurtainFrameNativeOwnershipService.RequireMatchingOwnership(
                        solid,
                        project,
                        element,
                        "validate generated curtain frame " + id.Handle);
                }
                validation.Commit();
            }
        }

        private static void EnsureCurtainPanelSetLive(
            Document document,
            ProjectState project,
            ProjectElement element,
            IReadOnlyList<string> expected)
        {
            var ids = ResolveCompleteSet(document, element, CurtainPanelHandlesKey, expected);
            using (var validation = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in ids)
                {
                    var entity = validation.GetObject(id, OpenMode.ForRead, false) as Entity;
                    var solid = entity as Solid3d;
                    if (solid == null || solid.IsErased)
                        throw new InvalidOperationException("Generated " + CurtainPanelHandlesKey + " for " + element.Id + " is not a live Solid3d. Refusing destructive invalidation.");
                    GeneratedCurtainPanelNativeOwnershipService.RequireMatchingOwnership(solid, project, element, "validate generated curtain panel " + id.Handle);
                }
                validation.Commit();
            }
        }

        private static void EnsureGridAnnotationsLive(Document document, ProjectState project, ProjectElement element)
        {
            if (!element.Properties.TryGetValue(GridAnnotationBuilder.HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            if (element.Category != ElementCategory.Grid)
                throw new InvalidOperationException(
                    "Generated Grid annotation metadata is attached to non-Grid element " + element.Id + ". Refusing destructive invalidation.");

            var expected = ParseExpectedHandles(raw, element, GridAnnotationBuilder.HandlesKey);
            foreach (var handle in expected)
                EnsureGridAnnotationOwned(project, element, handle);

            var ids = ResolveCompleteSet(document, element, GridAnnotationBuilder.HandlesKey, expected);
            using (var validation = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in ids)
                {
                    var entity = validation.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased)
                        throw new InvalidOperationException(
                            "Generated Grid annotation for " + element.Id + " resolved to a non-live Entity. Refusing destructive invalidation before any generated geometry is erased.");
                    if (!(entity is Line) && !(entity is Circle) && !(entity is DBText))
                        throw new InvalidOperationException(
                            "Generated Grid annotation handle " + id.Handle + " resolves to unsupported entity type " + entity.GetType().Name + ". Refusing destructive invalidation.");
                    GeneratedGeometryService.RequireMatchingOwnership(entity, project, element, "validate stale Grid annotation " + id.Handle);
                }
                validation.Commit();
            }
        }

        private static void EnsureSemanticTagsLive(Document document, ProjectState project, ProjectElement element)
        {
            if (!element.Properties.TryGetValue(GeneratedSemanticTagHealthService.HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;

            var expected = ParseExpectedHandles(raw, element, GeneratedSemanticTagHealthService.HandlesKey);
            foreach (var handle in expected)
                EnsureSemanticTagOwned(project, element, handle);

            var ids = ResolveCompleteSet(document, element, GeneratedSemanticTagHealthService.HandlesKey, expected);
            using (var validation = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var id in ids)
                {
                    var entity = validation.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased)
                        throw new InvalidOperationException(
                            "Generated Semantic Tag for " + element.Id + " resolved to a non-live Entity. Refusing destructive invalidation before any generated geometry is erased.");
                    if (!(entity is MText))
                        throw new InvalidOperationException(
                            "Generated Semantic Tag handle " + id.Handle + " is live but is not MText. Refusing destructive invalidation.");
                    GeneratedGeometryService.RequireMatchingOwnership(entity, project, element, "validate stale Semantic Tag " + id.Handle);
                }
                validation.Commit();
            }
        }

        private static IReadOnlyList<ObjectId> ResolveCompleteSet(
            Document document,
            ProjectElement element,
            string propertyKey,
            IReadOnlyList<string> expected)
        {
            var ids = CadHandleService.Resolve(document, expected);
            if (ids.Count != expected.Count)
                throw new InvalidOperationException(
                    "Generated " + propertyKey + " metadata for " + element.Id + " expects " + expected.Count +
                    " live CAD object(s), but only " + ids.Count +
                    " resolved. Refusing destructive invalidation before any generated geometry is erased.");
            return ids;
        }

        private static IReadOnlyList<string> ParseExpectedHandles(string raw, ProjectElement element, string propertyKey)
        {
            var result = new List<string>();
            var seenCanonical = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var token in (raw ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var metadataHandle = token.Trim();
                if (metadataHandle.Length == 0) continue;
                var canonical = CadHandleService.NormalizeHexHandle(metadataHandle);
                if (canonical == null)
                    throw new InvalidOperationException(
                        "Generated " + propertyKey + " handle '" + metadataHandle + "' for " + element.Id +
                        " is malformed. Refusing destructive invalidation before any generated geometry is erased.");
                if (seenCanonical.Add(canonical)) result.Add(metadataHandle);
            }

            if (result.Count == 0)
                throw new InvalidOperationException(
                    "Generated " + propertyKey + " metadata for " + element.Id +
                    " does not contain a valid CAD handle. Refusing destructive invalidation before any generated geometry is erased.");
            return result;
        }

        private static bool HasGeneratedRebar(ProjectElement element)
        {
            foreach (var key in CoreOwnershipPolicy.RebarHandleKeys)
                if (element.Properties.TryGetValue(key, out var raw) && !string.IsNullOrWhiteSpace(raw)) return true;
            return false;
        }

        private static bool HasCurtainFrames(ProjectElement element) =>
            element.Properties.TryGetValue(CurtainFrameHandlesKey, out var raw) && !string.IsNullOrWhiteSpace(raw);

        private static bool HasCurtainPanels(ProjectElement element) =>
            element.Properties.ContainsKey("GeneratedCurtainPanelBuildState") ||
            element.Properties.TryGetValue(CurtainPanelHandlesKey, out var raw) && !string.IsNullOrWhiteSpace(raw);

        private static IReadOnlyList<string> ParseCurtainPanelExpectedHandles(ProjectElement element)
        {
            if (!element.Properties.TryGetValue("GeneratedCurtainPanelBuildState", out var state) ||
                !string.Equals((state ?? string.Empty).Trim(), "Complete", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Generated curtain panel build state is missing or invalid for " + element.Id + ". Refusing destructive invalidation.");
            if (!element.Properties.TryGetValue("GeneratedCurtainPanelCount", out var countRaw) ||
                !int.TryParse(countRaw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var recordedCount) ||
                recordedCount < 0)
                throw new InvalidOperationException("Generated curtain panel count is missing or invalid for " + element.Id + ". Refusing destructive invalidation.");
            var hasHandles = element.Properties.TryGetValue(CurtainPanelHandlesKey, out var raw) && !string.IsNullOrWhiteSpace(raw);
            if (!hasHandles)
            {
                if (recordedCount == 0) return Array.Empty<string>();
                throw new InvalidOperationException("Generated curtain panel metadata for " + element.Id + " records " + recordedCount + " panels but has no handles. Refusing invalidation to avoid orphaning native solids.");
            }
            var expected = ParseExpectedHandles(raw ?? string.Empty, element, CurtainPanelHandlesKey);
            if (recordedCount != expected.Count)
                throw new InvalidOperationException("Generated curtain panel count does not match its exact handle set for " + element.Id + ". Refusing destructive invalidation.");
            return expected;
        }

        private static void EraseRebarSet(
            Document document,
            Transaction transaction,
            ProjectState project,
            ProjectElement element,
            string propertyKey,
            GeneratedRebarOwnershipGuard.OwnershipIndex ownership)
        {
            if (!element.Properties.TryGetValue(propertyKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            var expected = ParseExpectedHandles(raw, element, propertyKey);
            foreach (var handle in expected) ownership.EnsureOwned(handle, element, propertyKey);

            var ids = ResolveCompleteSet(document, element, propertyKey, expected);
            foreach (var id in ids)
            {
                var entity = transaction.GetObject(id, OpenMode.ForWrite, false) as Entity;
                if (entity == null || entity.IsErased)
                    throw new InvalidOperationException(
                        "Generated " + propertyKey + " handle " + id.Handle + " is no longer live. Refusing partial destructive invalidation.");
                var solid = entity as Solid3d;
                if (solid == null)
                    throw new InvalidOperationException(
                        "Generated " + propertyKey + " handle " + id.Handle + " is live but is not a Solid3d. Refusing destructive invalidation.");
                GeneratedRebarNativeOwnershipService.RequireMatchingOwnership(
                    solid,
                    project,
                    element,
                    propertyKey,
                    "erase stale generated rebar " + id.Handle);
                solid.Erase();
            }
        }

        private static void EraseCurtainFrames(
            Document document,
            Transaction transaction,
            ProjectState project,
            ProjectElement element,
            GeneratedCurtainFrameOwnershipGuard.OwnershipIndex ownership)
        {
            if (!element.Properties.TryGetValue(CurtainFrameHandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            var expected = ParseExpectedHandles(raw, element, CurtainFrameHandlesKey);
            foreach (var handle in expected) ownership.EnsureOwned(handle, element);

            var ids = ResolveCompleteSet(document, element, CurtainFrameHandlesKey, expected);
            foreach (var id in ids)
            {
                var entity = transaction.GetObject(id, OpenMode.ForWrite, false) as Entity;
                if (entity == null || entity.IsErased)
                    throw new InvalidOperationException(
                        "Generated " + CurtainFrameHandlesKey + " handle " + id.Handle + " is no longer live. Refusing partial destructive invalidation.");
                var solid = entity as Solid3d;
                if (solid == null)
                    throw new InvalidOperationException(
                        "Generated " + CurtainFrameHandlesKey + " handle " + id.Handle + " is live but is not a Solid3d. Refusing destructive invalidation.");
                GeneratedCurtainFrameNativeOwnershipService.RequireMatchingOwnership(
                    solid,
                    project,
                    element,
                    "erase stale generated curtain frame " + id.Handle);
                solid.Erase();
            }
        }

        private static void EraseCurtainPanels(
            Document document,
            Transaction transaction,
            ProjectState project,
            ProjectElement element,
            GeneratedCurtainPanelOwnershipGuard.OwnershipIndex ownership)
        {
            if (!HasCurtainPanels(element)) return;
            var expected = ParseCurtainPanelExpectedHandles(element);
            if (expected.Count == 0) return;
            foreach (var handle in expected) ownership.EnsureOwned(handle, element);
            var ids = ResolveCompleteSet(document, element, CurtainPanelHandlesKey, expected);
            foreach (var id in ids)
            {
                var entity = transaction.GetObject(id, OpenMode.ForWrite, false) as Entity;
                var solid = entity as Solid3d;
                if (solid == null || solid.IsErased)
                    throw new InvalidOperationException("Generated " + CurtainPanelHandlesKey + " handle " + id.Handle + " is not a live Solid3d. Refusing destructive invalidation.");
                GeneratedCurtainPanelNativeOwnershipService.RequireMatchingOwnership(solid, project, element, "erase stale generated curtain panel " + id.Handle);
                solid.Erase();
            }
        }

        private static void EraseGridAnnotations(Document document, Transaction transaction, ProjectState project, ProjectElement element)
        {
            if (!element.Properties.TryGetValue(GridAnnotationBuilder.HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            if (element.Category != ElementCategory.Grid)
                throw new InvalidOperationException("Generated Grid annotation metadata is attached to non-Grid element " + element.Id + ". Refusing destructive invalidation.");

            var expected = ParseExpectedHandles(raw, element, GridAnnotationBuilder.HandlesKey);
            foreach (var handle in expected)
                EnsureGridAnnotationOwned(project, element, handle);

            var ids = ResolveCompleteSet(document, element, GridAnnotationBuilder.HandlesKey, expected);
            foreach (var id in ids)
            {
                var entity = transaction.GetObject(id, OpenMode.ForWrite, false) as Entity;
                if (entity == null || entity.IsErased)
                    throw new InvalidOperationException(
                        "Generated Grid annotation handle " + id.Handle + " is no longer live. Refusing partial destructive invalidation.");
                if (!(entity is Line) && !(entity is Circle) && !(entity is DBText))
                    throw new InvalidOperationException("Generated Grid annotation handle " + id.Handle + " resolves to unsupported entity type " + entity.GetType().Name + ". Refusing destructive invalidation.");
                GeneratedGeometryService.RequireMatchingOwnership(entity, project, element, "erase stale Grid annotation " + id.Handle);
                entity.Erase();
            }
        }

        private static void EraseSemanticTags(Document document, Transaction transaction, ProjectState project, ProjectElement element)
        {
            if (!element.Properties.TryGetValue(GeneratedSemanticTagHealthService.HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;

            var expected = ParseExpectedHandles(raw, element, GeneratedSemanticTagHealthService.HandlesKey);
            foreach (var handle in expected)
                EnsureSemanticTagOwned(project, element, handle);

            var ids = ResolveCompleteSet(document, element, GeneratedSemanticTagHealthService.HandlesKey, expected);
            foreach (var id in ids)
            {
                var entity = transaction.GetObject(id, OpenMode.ForWrite, false) as Entity;
                if (entity == null || entity.IsErased)
                    throw new InvalidOperationException(
                        "Generated Semantic Tag handle " + id.Handle + " is no longer live. Refusing partial destructive invalidation.");
                if (!(entity is MText))
                    throw new InvalidOperationException(
                        "Generated Semantic Tag handle " + id.Handle + " is live but is not MText. Refusing destructive invalidation.");
                GeneratedGeometryService.RequireMatchingOwnership(entity, project, element, "erase stale Semantic Tag " + id.Handle);
                entity.Erase();
            }
        }

        private static void EnsureGridAnnotationOwned(ProjectState project, ProjectElement element, string handle)
        {
            if (!CoreOwnershipPolicy.TryFindOwner(project, handle, out var owner, out var propertyKey) || owner == null)
                throw new InvalidOperationException("Generated Grid annotation handle " + handle + " has no semantic owner. Refusing destructive invalidation.");
            if (!string.Equals(owner.Id, element.Id, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(CoreOwnershipPolicy.CanonicalOwnerSlot(propertyKey), CoreOwnershipPolicy.CanonicalOwnerSlot(GridAnnotationBuilder.HandlesKey), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Generated Grid annotation handle " + handle + " is owned by " + owner.Id + "/" + propertyKey +
                    ", not " + element.Id + "/" + GridAnnotationBuilder.HandlesKey + ". Refusing destructive invalidation.");
        }

        private static void EnsureSemanticTagOwned(ProjectState project, ProjectElement element, string handle)
        {
            if (!CoreOwnershipPolicy.TryFindOwner(project, handle, out var owner, out var propertyKey) || owner == null)
                throw new InvalidOperationException("Generated Semantic Tag handle " + handle + " has no semantic owner. Refusing destructive invalidation.");
            if (!ReferenceEquals(owner, element) ||
                !string.Equals(
                    CoreOwnershipPolicy.CanonicalOwnerSlot(propertyKey),
                    CoreOwnershipPolicy.CanonicalOwnerSlot(GeneratedSemanticTagHealthService.HandlesKey),
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Generated Semantic Tag handle " + handle + " is owned by " + owner.Id + "/" + propertyKey +
                    ", not " + element.Id + "/" + GeneratedSemanticTagHealthService.HandlesKey + ". Refusing destructive invalidation.");
        }
    }
}
