# Door/Opening source-instance generation fence

## Scope

This runbook covers managed Core correctness for `DoorOpeningScheduleBuilder`. No licensed BricsCAD runtime claim is required.

## Defect boundary

The Door/Opening schedule already captures detached semantic element/floor/family values and revalidates them while building. Before issue #6024, however, the captured revision did not retain the original `ProjectElement` instances. A value-equivalent replacement in the same list slot could therefore satisfy semantic comparison while representing a different project generation/lifecycle instance.

## Required invariant

A schedule generation snapshot must bind both:

1. detached semantic values consumed by aggregation/publication; and
2. exact source `ProjectElement` instance identity and list order.

`EnsureProjectRevision` must reject either semantic drift or value-equivalent source-instance replacement, even when `ProjectState.ChangeVersion` is unchanged.

## Regression

`DoorOpeningSourceInstanceFenceSmoke` captures the private schedule revision, replaces the first element with a semantically equivalent new instance without touching `ChangeVersion`, and verifies the private revision guard fails closed. Stable schedule generation remains covered in the same ModuleInitializer smoke.

## Static guard

`scripts/preflight-door-opening-source-instance-fence.py` pins the dual identity + semantic fence and the deterministic replacement regression.

## Acceptance

Run the focused preflight and managed Core smoke suite. For integration, require fresh protected PR `preflight` and `core` SUCCESS on the exact current candidate, latest-main freshness/collision validation, then merge only through the protected PR path. Do not infer any BricsCAD `LOCAL_PASS` from this managed validation.
