# Measurement/work-item mapping known-Count stability

## Scope

This package qualifies deterministic Core cardinality integrity for `MeasurementWorkItemMappingCatalog`. A mapping source may be streaming-only or may expose deterministic Count evidence through generic, read-only, or non-generic collection interfaces.

No BricsCAD host, private DWG, or licensed runtime is required.

## Defect boundary

The catalog previously observed known Count before enumeration and enforced traversal overrun/under-yield, but never re-observed Count after caller-controlled traversal. A counted source could therefore advertise `N`, yield exactly `N` valid mappings, change its Count during enumeration, and still publish a catalog whose cardinality provenance had changed.

## Hardened contract

Construction now:

1. preserves admission rejection for negative, conflicting, and over-limit known Count evidence;
2. preserves fail-early rejection before consuming an item beyond admitted Count;
3. preserves exact under-yield rejection after traversal;
4. rebinds every supported deterministic Count surface after exact traversal;
5. rejects negative, conflicting, missing, or changed post-traversal Count evidence before sorting or publishing `Mappings`;
6. preserves the independent 10,000-entry ceiling for streaming sources;
7. preserves duplicate mapping-id and ambiguous category/measurement-target checks.

## Deterministic regression

`MeasurementWorkItemMappingKnownCountStabilitySmoke` auto-runs and covers generic, read-only, and non-generic Count drift; negative and conflicting post-traversal evidence; stable counted input; and pure streaming input.

`preflight-measurement-work-item-mapping-known-count-stability.py` pins the source ordering so post-traversal Count rebinding cannot be moved after sort/publication.

## Validation

Run normal Shared Branch and protected PR CI on the exact candidate. Reconcile latest protected `main` non-force before merge if it moves. Merge only with terminal protected `preflight` + `core` success for the exact head and expected-head protection.

This package does not claim licensed BricsCAD runtime, private-DWG evidence, or `LOCAL_PASS`.
