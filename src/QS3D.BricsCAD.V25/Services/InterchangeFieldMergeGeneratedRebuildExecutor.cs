using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Services;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Services
{
    internal sealed class InterchangeFieldMergeNativeRebuildTarget
    {
        public InterchangeFieldMergeNativeRebuildTarget(string elementId, ElementCategory category, string sourceHandle)
        {
            ElementId = elementId ?? throw new ArgumentNullException(nameof(elementId));
            Category = category;
            SourceHandle = sourceHandle ?? throw new ArgumentNullException(nameof(sourceHandle));
        }

        public string ElementId { get; }
        public ElementCategory Category { get; }
        public string SourceHandle { get; }
    }

    internal sealed class InterchangeFieldMergeGeneratedRebuildManifest
    {
        public InterchangeFieldMergeGeneratedRebuildManifest(
            InterchangeFieldMergeGeneratedRebuildPlan plan,
            IReadOnlyList<InterchangeFieldMergeNativeRebuildTarget> nativeTargets)
        {
            Plan = plan ?? throw new ArgumentNullException(nameof(plan));
            NativeTargets = nativeTargets ?? throw new ArgumentNullException(nameof(nativeTargets));
        }

        public InterchangeFieldMergeGeneratedRebuildPlan Plan { get; }
        public IReadOnlyList<InterchangeFieldMergeNativeRebuildTarget> NativeTargets { get; }
    }

    internal sealed class InterchangeFieldMergeGeneratedRebuildResult
    {
        public InterchangeFieldMergeGeneratedRebuildResult(int nativeGeometryRebuilt, int semanticElementsRegenerated)
        {
            NativeGeometryRebuilt = nativeGeometryRebuilt;
            SemanticElementsRegenerated = semanticElementsRegenerated;
        }

        public int NativeGeometryRebuilt { get; }
        public int SemanticElementsRegenerated { get; }
    }

    /// <summary>
    /// Preflights and executes the bounded automatic rebuild that follows one reviewed FieldMerge.
    /// Prepare is observational and must run before native invalidation. Execute runs only after old
    /// ownership metadata has been cleared, while the caller still owns the outer CAD transaction.
    /// </summary>
    internal static class InterchangeFieldMergeGeneratedRebuildExecutor
    {
        private const string GeneratedSolidHandleKey = "GeneratedSolidHandle";

        public static InterchangeFieldMergeGeneratedRebuildManifest Prepare(
            Document document,
            ProjectState project,
            IEnumerable<ProjectElement> affectedElements,
            InterchangeFieldMergeGeneratedRebuildPlan plan)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (affectedElements == null) throw new ArgumentNullException(nameof(affectedElements));
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            var affectedById = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in affectedElements)
            {
                if (element == null) throw new InvalidOperationException("FieldMerge rebuild affected closure contains a null semantic element.");
                if (!affectedById.TryAdd(element.Id, element))
                    throw new InvalidOperationException("FieldMerge rebuild affected closure contains duplicate element id: " + element.Id + ".");
            }

            var nativeTargets = new List<InterchangeFieldMergeNativeRebuildTarget>();
            var claimedSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var elementId in plan.ElementIds)
            {
                if (!affectedById.TryGetValue(elementId, out var element))
                    throw new InvalidOperationException(
                        "FieldMerge rebuild plan escaped the reviewed affected closure: " + elementId + ". Re-plan and review the merge.");

                var ownerSlots = element.Properties
                    .Where(property =>
                        !string.IsNullOrWhiteSpace(property.Value) &&
                        GeneratedHandleOwnershipPolicy.IsOwnerSlot((property.Key ?? string.Empty).Trim()))
                    .Select(property => (property.Key ?? string.Empty).Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (!plan.Includes(InterchangeGeneratedOutputKind.NativeGeometry))
                {
                    if (ownerSlots.Length > 0)
                        throw new InvalidOperationException(
                            "FieldMerge would invalidate native generated ownership for " + element.Id +
                            " but NativeGeometry rebuild was not explicitly requested.");
                    continue;
                }

                var unsupportedOwnerSlot = ownerSlots.FirstOrDefault(key =>
                    !string.Equals(key, GeneratedSolidHandleKey, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(unsupportedOwnerSlot))
                    throw new InvalidOperationException(
                        "FieldMerge atomic rebuild does not support generated owner slot '" + unsupportedOwnerSlot +
                        "' on element " + element.Id + ". Refusing before destructive invalidation; use the specialized production rebuild workflow.");

                if (!element.Properties.TryGetValue(GeneratedSolidHandleKey, out var generatedSolidHandle) ||
                    string.IsNullOrWhiteSpace(generatedSolidHandle))
                    continue;

                if (!StructuralSolidBuilder.Supports(element.Category))
                    throw new InvalidOperationException(
                        "FieldMerge atomic native rebuild does not support category " + element.Category +
                        " for element " + element.Id + ". Refusing before destructive invalidation.");

                if (element.Category == ElementCategory.Slab &&
                    SlabOpeningPeerReplayService.CaptureAppliedOpeningIds(project, element, generatedSolidHandle).Count > 0)
                    throw new InvalidOperationException(
                        "FieldMerge atomic rebuild refuses Slab " + element.Id +
                        " because applied slabOpen peers require the retiring solid handle during specialized replay.");

                var sourceIds = CadHandleService.Resolve(document, element.SourceHandles);
                if (sourceIds.Count != 1)
                    throw new InvalidOperationException(
                        "FieldMerge atomic native rebuild requires exactly one live CAD source for element " + element.Id +
                        "; resolved " + sourceIds.Count + ".");

                var sourceHandle = sourceIds[0].Handle.ToString();
                if (!claimedSources.Add(sourceHandle))
                    throw new InvalidOperationException(
                        "FieldMerge atomic native rebuild found one CAD source claimed by multiple affected elements: " + sourceHandle + ".");

                nativeTargets.Add(new InterchangeFieldMergeNativeRebuildTarget(element.Id, element.Category, sourceHandle));
            }

            return new InterchangeFieldMergeGeneratedRebuildManifest(
                plan,
                nativeTargets
                    .OrderBy(target => target.Category)
                    .ThenBy(target => target.ElementId, StringComparer.OrdinalIgnoreCase)
                    .ToArray());
        }

        public static InterchangeFieldMergeGeneratedRebuildResult Execute(
            Document document,
            ProjectState project,
            InterchangeFieldMergeGeneratedRebuildManifest manifest)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            if (manifest.Plan.IsNoOp) return new InterchangeFieldMergeGeneratedRebuildResult(0, 0);

            var originalSelection = CaptureImpliedSelection(document);
            var nativeBuilt = 0;
            try
            {
                if (manifest.Plan.Includes(InterchangeGeneratedOutputKind.NativeGeometry))
                {
                    foreach (var categoryGroup in manifest.NativeTargets
                                 .GroupBy(target => target.Category)
                                 .OrderBy(group => group.Key))
                    {
                        var targets = categoryGroup
                            .OrderBy(target => target.ElementId, StringComparer.OrdinalIgnoreCase)
                            .ToArray();
                        var sourceIds = new List<ObjectId>(targets.Length);
                        foreach (var target in targets)
                        {
                            var current = project.FindElement(target.ElementId)
                                ?? throw new InvalidOperationException(
                                    "FieldMerge rebuild target disappeared after semantic apply: " + target.ElementId + ".");
                            if (current.Category != target.Category)
                                throw new InvalidOperationException(
                                    "FieldMerge rebuild target category changed after review: " + target.ElementId + ". Re-plan and review the merge.");

                            var resolved = CadHandleService.Resolve(document, new[] { target.SourceHandle });
                            if (resolved.Count != 1 ||
                                !current.SourceHandles.Any(handle =>
                                    string.Equals(
                                        CadHandleService.NormalizeHexHandle(handle),
                                        CadHandleService.NormalizeHexHandle(target.SourceHandle),
                                        StringComparison.OrdinalIgnoreCase)))
                                throw new InvalidOperationException(
                                    "FieldMerge rebuild source ownership changed after review for element " + target.ElementId + ".");
                            sourceIds.Add(resolved[0]);
                        }

                        document.Editor.SetImpliedSelection(sourceIds.ToArray());
                        var built = StructuralSolidBuilder.BuildSelected(document, project, categoryGroup.Key);
                        if (built != targets.Length)
                            throw new InvalidOperationException(
                                "FieldMerge atomic native rebuild produced " + built + " of " + targets.Length +
                                " expected " + categoryGroup.Key + " solids. Aborting the outer FieldMerge transaction.");
                        nativeBuilt += built;
                    }
                }

                var regenerated = 0;
                if (manifest.Plan.Includes(InterchangeGeneratedOutputKind.Quantity))
                {
                    var engine = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault());
                    regenerated = engine.RegenerateDirtySubset(project, manifest.Plan.ElementIds);
                }

                return new InterchangeFieldMergeGeneratedRebuildResult(nativeBuilt, regenerated);
            }
            finally
            {
                RestoreImpliedSelectionBestEffort(document, originalSelection);
            }
        }

        private static ObjectId[] CaptureImpliedSelection(Document document)
        {
            try
            {
                var selection = document.Editor.SelectImplied();
                return selection.Status == PromptStatus.OK && selection.Value != null
                    ? selection.Value.GetObjectIds()
                    : Array.Empty<ObjectId>();
            }
            catch
            {
                return Array.Empty<ObjectId>();
            }
        }

        private static void RestoreImpliedSelectionBestEffort(Document document, ObjectId[] objectIds)
        {
            try { document.Editor.SetImpliedSelection(objectIds ?? Array.Empty<ObjectId>()); }
            catch
            {
                try { CadHandleService.ClearSelection(document); }
                catch { }
            }
        }
    }
}
