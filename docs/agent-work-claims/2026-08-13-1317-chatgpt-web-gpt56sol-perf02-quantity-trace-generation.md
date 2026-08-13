# Work claim — PERF-02 managed quantity-trace generation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-perf02-quantity-trace-20260813-1317`
- Registered: `2026-08-13T13:17:38+07:00`
- Baseline main SHA: `03923d284c4f6351fbdee3c96892200cbc04a0e4`
- Priority: `PERF-02 / P0` — add the explicitly queued managed quantity-trace workflow to the existing bounded Core large-model harness

## Confirmed gap

The canonical `tests/QS3D.Core.PerfHarness/Program.cs` currently measures dependency rebuild/closure, `MarkChanged`, and targeted regeneration only. The current workstream explicitly lists `quantity trace generation` as an independently measurable `PERF-02` workflow. MTR-03 now exposes canonical `QuantityEngine.CalculateWithTrace(...)`; current source/history and claim audit found no performance scenario for that path.

## Reserved scope

Extend the existing Core perf harness with one `quantity-trace-generation` scenario. The scenario must use the existing `QuantityEngine.CalculateWithTrace(...)` API and bounded deterministic `EntitySnapshot` fixtures, validate deterministic/parity invariants outside and inside the measurement path, and avoid retaining all generated traces.

## Expected surfaces

- `tests/QS3D.Core.PerfHarness/Program.cs` — add the scenario, deterministic fixture/checksum validation, CLI help/allowlist text only.
- this claim file.

The existing generic `scripts/run-core-performance.ps1` already forwards `--scenario`; it is intentionally not reserved or modified unless a post-claim source read proves a required compatibility defect.

## Excluded scope

- No edits to `MeasurementTrace`, `MeasurementSnapshot`, `QuantityEngine`, `TakeoffResultWithTrace`, `QuantityRule*`, semantic regenerators, persistence, report/UI or native adapters.
- No second performance harness, quantity engine, unit-conversion path, trace serializer or report path.
- No timing threshold presented as native/product readiness; this is managed Core measurement infrastructure only.
- No GitHub Actions dispatch and no BricsCAD V25/V26 native PASS claim.
- Active REV-01A Measurement Snapshot, MTR/Rules, LOCAL-003 Level and Curtain P11 claims remain fully excluded.

## Validation plan

- Publish this claim alone on current `main`, re-fetch and recheck new ACTIVE/BLOCKED claims before source work.
- Keep fixtures explicitly bounded by the existing `--elements <= 250000` contract; build source snapshots once per scenario and consume one trace result at a time rather than collecting trace outputs.
- Verify two deterministic pre-measurement passes produce the same processed count/checksum and that every result has exact Result/Trace gross/net/unit/source parity with the canonical API.
- Preserve the existing JSON evidence schema and runner contract; only add the new benchmark result name.
- Re-fetch and compare the implementation commit from current `main` after push.
- Connector-only source/readback is not an executable `.NET` perf run. No managed timing/PASS will be claimed unless the harness is actually executed in an available checkout.

## Coordination

- Current full ACTIVE/BLOCKED claim audit plus current-main deltas show no reservation of `tests/QS3D.Core.PerfHarness/Program.cs` or the quantity-trace performance workflow.
- The active REV-01A lane owns new Measurement Snapshot files only; the MTR/Rules lanes own Measurement/Rules surfaces; LOCAL claims own native/runtime surfaces. This lane consumes their public canonical APIs without editing them.
- The historical Core perf harness (`63733a75b6af4edaa3b02f7d19da5f8e82222d71`) already established the canonical bounded harness and generic exact-SHA runner; this claim extends that infrastructure rather than replacing it.

## Completion condition

A claim-first, bounded `quantity-trace-generation` scenario is present on current `main`, uses only canonical trace calculation, has deterministic correctness/resource-bound guards, preserves the existing runner/evidence schema, and this claim is marked `COMPLETED` with exact implementation SHA plus validation actually executed.