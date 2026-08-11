# Work claim — dependency health canonical relation blockers

- Status: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-dependency-health-canonical-relations`
- Registered: `2026-08-12T00:47:00+07:00`
- Baseline main SHA: `47c0d4e1e160b913d72cf76857362abd8c329be3`
- Priority: deterministic Health/Release diagnostic mismatch with the canonical DependencyGraph contract

## Confirmed defect

`DependencyGraph` now fails closed on blank, padded and case-insensitive duplicate dependency entries, and full graph construction rejects unresolved targets. `DependencyHealthService`, which is consumed by Health All and Release Check, still trims padded dependency text and silently deduplicates same-element dependency IDs. A project can therefore receive no dependency-health Error for relation state that regeneration/graph construction will reject.

`ModelHealthService` also normalizes dependency text and currently emits only a Warning for duplicate dependencies, so it does not close this Release-blocker mismatch by itself.

## Reserved scope

Harden `DependencyHealthService.Inspect(...)` only:

- a nonblank dependency entry with leading/trailing whitespace emits one `DEPENDENCY_TARGET_NON_CANONICAL` Error for the referencing element and is not traversed/normalized into the graph;
- a second canonical case-insensitive occurrence of the same dependency ID on one element emits one `DEPENDENCY_TARGET_DUPLICATE` Error and is not traversed a second time;
- blank, missing, ambiguous, self-reference and cycle behavior remains unchanged for otherwise canonical entries;
- diagnostics remain read-only and deterministic.

## Expected surfaces

- `src/QS3D.Core/Diagnostics/DependencyHealthService.cs`
- `tests/QS3D.Core.SmokeTests/DependencyHealthCanonicalRelationSmoke.cs`
- module-initializer registration in the new smoke file
- this claim file

## Excluded scope

- No `DependencyGraph`, `ModelHealthService`, Health All, Release Check, RegenerationEngine, Build3D, persistence, relation repair or native/V25 changes.
- No change to missing/ambiguous/self/cycle semantics or severity.
- No GitHub Actions dispatch.

## Validation plan

- Padded dependency referencing an existing element yields `DEPENDENCY_TARGET_NON_CANONICAL` Error and is not misclassified as missing/cycle.
- Case-insensitive duplicate canonical dependency entries yield exactly one `DEPENDENCY_TARGET_DUPLICATE` Error while the first edge remains available for cycle analysis.
- Canonical unique dependencies remain healthy.
- Existing missing-target regression still sees exactly one missing issue for its canonical `MISSING-TARGET` token even when separate padded tokens are present.
- Health inspection does not mutate `DependsOn`, project ChangeVersion or element timestamps.
- Inspect exact implementation diff and re-fetch source/test from current `main` before close-out.

## Coordination

The dependency-graph canonical-relation lane (`9de690b278db71664218d2ea9360d0d3a84993e6`) and full-graph referential-integrity lane (`7f2849a0f8ddc4d76d26ae47184e855974068099`) are complete. The immediately preceding smoke-alignment lane (`122fe267d75e3d3dd71d681e2b64a7dc32f46499`) is also complete. Recent commit search found no active claim for dependency-health padded/duplicate relation blockers.

## Completion condition

Current `main` makes Health/Release surface Error diagnostics for canonical relation defects that `DependencyGraph` rejects, without changing other dependency-health semantics, with focused deterministic regression coverage and this claim closed `COMPLETED`.
