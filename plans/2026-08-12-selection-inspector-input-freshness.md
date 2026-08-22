# Selection Inspector input-enumeration freshness plan

## Goal

Prevent `SemanticSelectionInspector.Inspect(...)` from returning a snapshot assembled across two project revisions when the caller-supplied lazy selection enumerable mutates the project while it is being enumerated.

## Implementation

1. Re-fetch current `main` and `SemanticSelectionInspector.cs` after claim registration.
2. Capture `project.ChangeVersion` immediately before enumerating the external `elementIds` sequence.
3. After materializing/validating the requested ids, fail closed if `ChangeVersion` changed before any result projection is built.
4. Add focused Core smoke coverage with a lazy enumerable that mutates project state between yielded ids, plus a normal inspection non-regression case.
5. Verify moving-main ancestry/no source overlap and close the claim with exact SHAs.

## Exclusions

No selection ordering changes, no property/family/quantity semantics changes, no bulk-edit mutation changes, no BricsCAD adapter changes and no Actions/build/release dispatch.
