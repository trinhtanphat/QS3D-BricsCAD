# 2D takeoff revision compare: distinct revision boundary

CostX/Autodesk-style drawing revision overlay represents a transition between two logical drawing revisions. `DrawingRevisionComparer2D.Compare` therefore now fails closed when both snapshots carry the same revision identifier, using case-insensitive comparison.

## Compatibility

Valid comparisons between different revisions are unchanged. Existing Added, Removed, Changed, and Unchanged classifications remain deterministic by markup id, and `QuantityDelta` remains signed as `current - previous`.

The check is intentionally limited to revision identity. Source references may differ for the same revision, but that does not make a valid revision transition; callers should publish a new revision identifier before requesting an overlay/compare. Conversely, distinct revisions may use any valid source references already accepted by the sheet-ingestion boundary.

## Migration

Callers that previously compared two snapshots labeled with the same revision must either avoid invoking revision compare for a republished-but-logically-identical revision, or assign the authoritative new revision identifier before extraction/comparison. No changes are required for genuine R1 → R2 (or equivalent) workflows.
