# Work claim — Regeneration profiler subset bounded targets

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-regeneration-profile-target-bound`
- Registered: `2026-08-12T00:06:00+07:00`
- Baseline main SHA: `ea121c260448f2b83311b141a79052c081446955`
- Priority: P1 — read-only regeneration profiling must not consume impossible target subsets without bound.

## Confirmed defect

`RegenerationWorkProfiler.ProfileSubset(...)` validates `project` first, but `CanonicalTargetIds(...)` still consumes the entire caller-provided `IEnumerable<string>` into a list before resolving targets. A valid unique target set can never exceed `project.Elements.Count`; nevertheless an oversized or non-terminating unique sequence can consume unbounded time/memory before the profiler reaches unknown-target resolution.

The current project element cardinality is the exact semantic maximum, matching the bounded apply/preview contracts without inventing a new product limit.

## Reserved scope

- `src/QS3D.Core/Services/RegenerationWorkProfiler.cs`
- `tests/QS3D.Core.SmokeTests/RegenerationWorkProfilerTargetBoundSmoke.cs` (new auto-registered focused smoke)
- this claim file

## Intended contract

- Unique profile subset target enumeration stops before accepting target `project.Elements.Count + 1`.
- Blank/padded/duplicate diagnostics keep their current precedence.
- Empty subsets and exact-cardinality valid subsets retain existing behavior.
- Profiling order, freshness, dependency metrics, DTO semantics, and native behavior are unchanged.

## Coordination

The earlier regeneration profile DTO integrity lane is completed and addressed DTO invariant validation, not arbitrary `ProfileSubset` target enumeration. The apply and preview target-bound lanes are separate and already completed. No recent exact claim was found for this profiler input path.

## Validation plan

- Add an auto-registered sentinel smoke proving a two-element project rejects the third unique profile target before requesting a fourth.
- Verify exact-cardinality profiling remains accepted and duplicate diagnostics retain precedence.
- SHA-guard source write, inspect exact published diffs, and close this claim.
- No GitHub Actions dispatch; no executable .NET or BricsCAD V25 runtime PASS claim from this hosted environment.

## Completion condition

Profiler subset input is bounded to the largest possible valid target set, focused regression is on `main`, and this claim is closed.
