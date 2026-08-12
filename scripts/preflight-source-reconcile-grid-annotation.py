#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
INVALIDATOR = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs"
SOURCE_RECONCILE = ROOT / "src/QS3D.BricsCAD.V25/Services/SourceReconcileService.cs"
GRID_BUILDER = ROOT / "src/QS3D.BricsCAD.V25/Cad/GridAnnotationBuilder.cs"

text = INVALIDATOR.read_text(encoding="utf-8")
reconcile = SOURCE_RECONCILE.read_text(encoding="utf-8")
builder = GRID_BUILDER.read_text(encoding="utf-8")

required = {
    "grid annotation owner slot": "GridAnnotationBuilder.HandlesKey",
    "semantic owner ambiguity guard": "CoreOwnershipPolicy.TryFindOwner(project, handle",
    "owner element verification": "owner.Id, element.Id",
    "owner slot verification": "CoreOwnershipPolicy.CanonicalOwnerSlot(propertyKey)",
    "xdata ownership verification": "GeneratedGeometryService.RequireMatchingOwnership(entity, project, element",
    "grid entity type guard": "entity is Line",
    "grid circle type guard": "entity is Circle",
    "grid text type guard": "entity is DBText",
    "grid CAD erase": "entity.Erase();",
    "grid metadata cleanup": 'RemoveByPrefix(element, "GeneratedGridAnnotation")',
    "semantic tag metadata cleanup": 'RemoveByPrefix(element, "GeneratedSemanticTag")',
    "semantic tag live validation": "EnsureSemanticTagsLive(document, project, element);",
    "semantic tag transactional erase": "EraseSemanticTags(document, transaction, project, element);",
    "semantic tag owner slot": "GeneratedSemanticTagHealthService.HandlesKey",
    "semantic tag type guard": "entity is MText",
    "semantic tag ownership verification": 'GeneratedGeometryService.RequireMatchingOwnership(entity, project, element, "erase stale Semantic Tag " + id.Handle)',
}
for label, token in required.items():
    if token not in text:
        raise SystemExit(f"source-reconcile grid annotation preflight failed: missing {label}: {token}")

for token in (
    "internal static void RebuildInTransaction(",
    "Transaction transaction,",
    "ProjectElement element)",
    "ReplaceOne(document, transaction, project, element);",
):
    if token not in builder:
        raise SystemExit("source-reconcile grid annotation preflight failed: transaction-local Grid rebuild contract missing: " + token)

for token in (
    "var annotatedGridTargets = invalidationTargets.Where(HasGridAnnotationIntent).ToList();",
    "private static bool HasGridAnnotationIntent(ProjectElement element)",
    "element.Category == ElementCategory.Grid",
    "element.Properties.TryGetValue(GridAnnotationBuilder.HandlesKey, out var raw)",
    "foreach (var grid in annotatedGridTargets)",
    "GridAnnotationBuilder.RebuildInTransaction(document, transaction, project, grid);",
):
    if token not in reconcile:
        raise SystemExit("source-reconcile grid annotation preflight failed: Grid annotation intent-preservation contract missing: " + token)

order = [
    "var annotatedGridTargets = invalidationTargets.Where(HasGridAnnotationIntent).ToList();",
    "GeneratedDependentGeometryInvalidator.Prepare",
    "RefreshSourceDerivedState",
    "RegenerateAffectedToStable",
    "invalidation.CommitMetadata();",
    "GridAnnotationBuilder.RebuildInTransaction(document, transaction, project, grid);",
    "transaction.Commit();",
]
pos = [reconcile.find(token) for token in order]
if any(value < 0 for value in pos) or pos != sorted(pos):
    raise SystemExit("source-reconcile grid annotation preflight failed: reconcile transaction ordering changed")
if "ProjectStateSnapshot.Capture(project)" not in reconcile or "rollback.Restore(project)" not in reconcile:
    raise SystemExit("source-reconcile grid annotation preflight failed: semantic rollback boundary missing")

print("source-reconcile grid annotation preflight: OK")
