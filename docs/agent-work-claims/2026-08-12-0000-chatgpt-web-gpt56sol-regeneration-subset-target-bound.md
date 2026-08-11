# Work claim — Regeneration subset target bounded enumeration

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-regeneration-subset-target-bound`
- Registered: `2026-08-12T00:00:00+07:00`
- Baseline main SHA: `17273180af0868afe06b724e1101db1de1e4b9d6`
- Priority: P1 — targeted regeneration input must be bounded before materializing arbitrary caller enumerables.

## Confirmed defect

`RegenerationEngine.RegenerateDirtySubset(...)` passes the caller-provided `IEnumerable<string>` to `CanonicalTargetIds(...)`, which enumerates the entire sequence into a `HashSet<string>` before any project resolution occurs. A valid unique target set can never contain more IDs than `project.Elements.Count`, yet an excessively large or non-terminating unique enumerable can currently consume unbounded time/memory before the engine reports an unknown target.

The exact project element cardinality is a natural semantic upper bound, so this can be hardened without inventing a new product limit.

## Reserved scope

- `src/QS3D.Core/Services/RegenerationEngine.cs`
- `tests/QS3D.Core.SmokeTests/RegenerationSubsetTargetBoundSmoke.cs` (new auto-registered focused smoke)
- this claim file

## Intended contract

- `RegenerateDirtySubset(...)` stops consuming unique target IDs once accepting another target would exceed the current project element count.
- Existing canonical blank/padded/duplicate validation remains intact and duplicate diagnostics take precedence over cardinality when the repeated ID itself is encountered.
- A target set at exactly project cardinality remains valid and normal unknown-target resolution semantics are unchanged inside that bound.
- No regenerator selection, dirty-state, quantity, dependency, rollback, or native behavior changes.

## Coordination

Recent regeneration work covers profile DTO integrity, semantic numeric fail-closed behavior, and null-regenerator validation; those lanes are completed and do not reserve this subset-target input path. Current recent claims concern recognition enumeration, dependency-impact freshness, grids, quantity settings, and unrelated surfaces.

## Validation plan

- Add an auto-registered Core smoke using a sentinel enumerable to prove a project with two elements rejects the third unique target without requesting a fourth item.
- Verify two exact in-project targets remain accepted.
- Verify duplicate target validation remains duplicate-specific rather than being masked by the cardinality guard.
- Re-fetch source immediately before update, use the current blob SHA, inspect exact diff, and close the claim with published SHAs.
- No GitHub Actions dispatch; no executable .NET or BricsCAD V25 runtime PASS claim from this hosted environment.

## Completion condition

Targeted regeneration no longer eagerly consumes target sequences beyond the maximum possible valid project subset, focused regression is on `main`, and this claim is closed.
