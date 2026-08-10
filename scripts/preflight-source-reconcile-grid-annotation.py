#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
INVALIDATOR = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs"
SOURCE_RECONCILE = ROOT / "src/QS3D.BricsCAD.V25/Services/SourceReconcileService.cs"

text = INVALIDATOR.read_text(encoding="utf-8")
reconcile = SOURCE_RECONCILE.read_text(encoding="utf-8")

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
}
for label, token in required.items():
    if token not in text:
        raise SystemExit(f"source-reconcile grid annotation preflight failed: missing {label}: {token}")

if 'RemoveByPrefix(element, "GeneratedSemanticTag")' in text:
    raise SystemExit("source-reconcile grid annotation preflight failed: semantic tags must not be removed by generic source reconcile")
if "GeneratedSemanticTagHandles" in text:
    raise SystemExit("source-reconcile grid annotation preflight failed: semantic tag handles must remain outside spatial generated-output invalidation")

order = [
    "GeneratedDependentGeometryInvalidator.Prepare",
    "RefreshSourceDerivedState",
    "RegenerateAffectedToStable",
    "invalidation.CommitMetadata();",
    "transaction.Commit();",
]
pos = [reconcile.find(token) for token in order]
if any(value < 0 for value in pos) or pos != sorted(pos):
    raise SystemExit("source-reconcile grid annotation preflight failed: reconcile transaction ordering changed")
if "ProjectStateSnapshot.Capture(project)" not in reconcile or "rollback.Restore(project)" not in reconcile:
    raise SystemExit("source-reconcile grid annotation preflight failed: semantic rollback boundary missing")

print("source-reconcile grid annotation preflight: OK")
