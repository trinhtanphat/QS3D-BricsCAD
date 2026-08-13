# Work claim — PERF-02 managed quantity-trace generation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-perf02-quantity-trace-20260813-1317`
- Registered: `2026-08-13T13:17:38+07:00`
- Baseline main SHA: `03923d284c4f6351fbdee3c96892200cbc04a0e4`
- Priority: `PERF-02 / P0` — add the explicitly queued managed quantity-trace workflow to the existing bounded Core large-model harness

## Confirmed gap

The canonical `tests/QS3D.Core.PerfHarness/Program.cs` measured dependency rebuild/closure, `MarkChanged`, and targeted regeneration only. The current workstream explicitly lists `quantity trace generation` as an independently measurable `PERF-02` workflow. MTR-03 exposes canonical `QuantityEngine.CalculateWithTrace(...)`; source/history and claim audit found no prior performance scenario for that path.

## Reserved scope

Extend the existing Core perf harness with one `quantity-trace-generation` scenario using the existing `QuantityEngine.CalculateWithTrace(...)` API and bounded deterministic `EntitySnapshot` fixtures. The scenario validates projection/determinism outside timing and avoids retaining all generated traces.

## Expected surfaces

- `tests/QS3D.Core.PerfHarness/Program.cs` — scenario, deterministic fixture/checksum validation and CLI help/allowlist text only.
- this claim file.

The existing generic `scripts/run-core-performance.ps1` already forwards `--scenario`; it was not modified.

## Excluded scope

- No edits to `MeasurementTrace`, `MeasurementSnapshot`, `QuantityEngine`, `TakeoffResultWithTrace`, `QuantityRule*`, semantic regenerators, persistence, report/UI or native adapters.
- No second performance harness, quantity engine, unit-conversion path, trace serializer or report path.
- No timing threshold presented as native/product readiness; this is managed Core measurement infrastructure only.
- No GitHub Actions dispatch and no BricsCAD V25/V26 native PASS claim.
- REV-01A Measurement Snapshot, MTR/Rules, LOCAL-003 Level and Curtain P11 claims remained fully excluded.

## Implementation

- Claim-only commit on `main`: `234986754fdbdb30552c6ae096507a5ac436a817` — `chore(agent): claim PERF-02 quantity-trace generation`.
- Implementation commit on `main`: `6529523db04c90293790c4336c6714ad9a98812c` — `perf(core): measure canonical quantity-trace generation`.
- Exact compare against the implementation parent confirmed the implementation commit changes only `tests/QS3D.Core.PerfHarness/Program.cs`.
- The existing JSON evidence schema remains version 1 and the existing exact-SHA PowerShell runner remains unchanged; the new scenario is available through its already-generic `--scenario` forwarding.

## Implemented scenario invariants

- `quantity-trace-generation` and `all` now execute a deterministic fixture covering Count/Length/Area/Volume in a repeating explicit sequence.
- Source fixtures are created once per scenario and remain bounded by the existing `--elements <= 250000` CLI contract.
- Each fixture uses finite non-negative drawing-unit metrics and the canonical `DrawingUnit.Millimeter` conversion input.
- Full pre-measurement validation requires Result/Trace kind, source identity, gross/net/unit, quantity key, rounding policy, adjustment count, fact/assumption cardinality and raw fact value/source parity.
- Two pre-measurement passes must produce the same processed count, fact count and stable custom checksum.
- Warmup/measured passes consume one `TakeoffResultWithTrace` at a time, compute the same deterministic checksum and do not collect generated traces, so trace output retention is O(1) beyond the bounded source fixture array.
- The checksum uses explicit numeric bits/string characters rather than randomized runtime `string.GetHashCode()`.
- No copied unit conversion or quantity formula was added; every measured item calls `QuantityEngine.CalculateWithTrace(...)` directly.

## Validation actually executed

- Re-fetched current `main` before claim, after claim, before implementation and after implementation; reconciled concurrent REV/LOCAL changes and confirmed no perf overlap.
- Verified the claim commit itself contains only this claim file.
- Verified exact implementation diff is one file only and re-read the full updated `Program.cs` from current `main`, including scenario dispatch, fixture bounds, parity checks, checksum path, CLI help and unchanged report schema.
- Verified `scripts/run-core-performance.ps1` is already generic and therefore required no source change.
- Performed static draft balance/syntax sanity for braces/parentheses before push.
- Attempted to locate a local .NET SDK with `dotnet --version`; the current container reports `dotnet: command not found`, so no executable build/performance run was available.
- Not executed: `.NET` build, `QS3D.Core.PerfHarness` execution/timing, GitHub Actions, BricsCAD V25/V26 runtime or licensed/native qualification. No PASS/timing claim is made for those unexecuted gates.

## Remaining executable gate

Run the existing exact-SHA runner in a checkout with .NET 8, for example the `quantity-trace-generation` scenario at an explicitly chosen bounded element count, and preserve its ignored JSON median/p95/allocation/working-set evidence. That managed evidence remains distinct from any BricsCAD native qualification.

## Coordination

- The current claim audit found no reservation of `tests/QS3D.Core.PerfHarness/Program.cs` or the quantity-trace performance workflow.
- Concurrent REV/MTR/Rules lanes own their Core contract files; LOCAL lanes own native/runtime surfaces. This implementation only consumes public canonical takeoff/trace APIs.
- The historical Core perf harness (`63733a75b6af4edaa3b02f7d19da5f8e82222d71`) remains the single performance infrastructure; this lane extended it rather than replacing it.

## Completion condition

Satisfied for this narrow `PERF-02` lane: a claim-first, bounded `quantity-trace-generation` scenario is present on current `main`, uses only canonical trace calculation, contains deterministic correctness/resource-bound guards, preserves the existing runner/evidence schema, and unexecuted managed/native gates are recorded without false PASS claims.