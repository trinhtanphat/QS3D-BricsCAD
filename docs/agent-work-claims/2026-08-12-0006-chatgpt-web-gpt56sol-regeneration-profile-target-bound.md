# Work claim — Regeneration profiler subset bounded targets

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-regeneration-profile-target-bound`
- Registered: `2026-08-12T00:06:00+07:00`
- Completed: `2026-08-12T00:08:00+07:00`
- Baseline main SHA: `ea121c260448f2b83311b141a79052c081446955`
- Reservation commit: `e22ce35530a78df4a536c7d2bf1eeb908d91b593`
- Priority: P1 — read-only regeneration profiling must not consume impossible target subsets without bound.

## Defect fixed

`RegenerationWorkProfiler.ProfileSubset(...)` validated `project` first, but `CanonicalTargetIds(...)` still consumed the entire caller-provided `IEnumerable<string>` into a list before resolving targets. A valid unique target set can never exceed `project.Elements.Count`; nevertheless an oversized or non-terminating unique sequence could consume unbounded time/memory before the profiler reached unknown-target resolution.

Canonical target materialization now receives `project.Elements.Count` as the exact maximum possible valid subset size. Blank/padded and duplicate diagnostics remain ahead of the cardinality check.

## Reserved scope

- `src/QS3D.Core/Services/RegenerationWorkProfiler.cs`
- `tests/QS3D.Core.SmokeTests/RegenerationWorkProfilerTargetBoundSmoke.cs`
- this claim file

## Published commits

- `6ddc5adee474756ce7a02c07b76d8fb9bd449f4e` — bound unique profiler subset target enumeration by current project element cardinality.
- `3063f286b98d34b1d3bdfa14a920fd8c98923ed6` — add isolated auto-registered smoke for sentinel non-overenumeration, exact-cardinality acceptance, and duplicate precedence.

## Delivered contract

- Profile subset cannot consume unique target IDs beyond the largest subset that could possibly resolve in the current project.
- Empty and exact-cardinality subsets retain their prior behavior.
- Duplicate/canonical target diagnostics keep their existing semantics.
- Profiling order, freshness checks, dependency metrics, profile DTO behavior, and native behavior are unchanged.

## Validation notes

- Exact source/test diffs were fetched after publication and are limited to the reserved surfaces.
- The sentinel regression would expose the prior over-enumeration path; the new implementation rejects the first impossible unique target without requesting the next item.
- Dedicated smoke auto-registers via `ModuleInitializer`; shared smoke registration was not edited.
- No force-push and no GitHub Actions dispatch.
- This hosted environment does not provide the repository .NET/BricsCAD V25 qualification toolchain, so executable/native runtime PASS is not claimed.

## Completion condition

Satisfied for the remote-safe source/static contract. Exact executable/native qualification remains separate.
