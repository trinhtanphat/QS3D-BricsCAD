# Work claim — Regeneration subset target bounded enumeration

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-regeneration-subset-target-bound`
- Registered: `2026-08-12T00:00:00+07:00`
- Completed: `2026-08-12T00:02:00+07:00`
- Baseline main SHA: `17273180af0868afe06b724e1101db1de1e4b9d6`
- Reservation commit: `3f2a706b82d37e453d9fbb6ee2335d5053ae6160`
- Priority: P1 — targeted regeneration input must be bounded before materializing arbitrary caller enumerables.

## Defect fixed

`RegenerationEngine.RegenerateDirtySubset(...)` previously passed the caller-provided `IEnumerable<string>` to `CanonicalTargetIds(...)`, which enumerated the entire sequence into a `HashSet<string>` before any project resolution occurred. A valid unique target set can never contain more IDs than `project.Elements.Count`, yet an excessively large or non-terminating unique enumerable could consume unbounded time/memory before the engine reported an unknown target.

Target canonicalization now receives the current project element count and rejects the next unique ID when accepting it would exceed that exact semantic maximum. Blank/padded validation and duplicate detection remain ahead of the cardinality check, so their existing diagnostics retain precedence.

## Reserved scope

- `src/QS3D.Core/Services/RegenerationEngine.cs`
- `tests/QS3D.Core.SmokeTests/RegenerationSubsetTargetBoundSmoke.cs`
- this claim file

## Published commits

- `a9f5f55dccd153f27abd13484a4230d9fc199e33` — bound unique subset target enumeration by `project.Elements.Count` while preserving canonical/duplicate validation order.
- `956d25e0c861b4f88e1126a98a70b73f6d35247f` — add isolated auto-registered smoke covering sentinel non-overenumeration, exact-cardinality acceptance, and duplicate precedence.

## Delivered contract

- `RegenerateDirtySubset(...)` cannot eagerly consume unique target IDs beyond the largest subset that could possibly resolve in the current project.
- No valid target subset is rejected by the new bound.
- Duplicate target validation remains duplicate-specific even when the project cardinality has already been reached.
- Regenerator selection, dirty-state, quantity, dependency, rollback, and native behavior are unchanged.

## Validation notes

- Exact source and smoke diffs were fetched after publication and are limited to the reserved surfaces.
- The sentinel regression is structured so the previous implementation would enumerate once beyond the possible project bound and throw the sentinel exception; the new implementation rejects the third unique ID for a two-element project before requesting another item.
- The focused smoke auto-registers via `ModuleInitializer`; no shared smoke registry was edited.
- No force-push and no GitHub Actions dispatch.
- This hosted environment does not provide the repository .NET/BricsCAD V25 qualification toolchain, so executable/native runtime PASS is not claimed.

## Completion condition

Satisfied for the remote-safe source/static contract. Exact executable/native qualification remains separate.
