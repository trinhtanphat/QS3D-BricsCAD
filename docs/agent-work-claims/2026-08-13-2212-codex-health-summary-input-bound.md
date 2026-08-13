# Work claim — HealthSummary bounded issue enumeration

- Status: `ACTIVE`
- Agent: `Codex / GPT-5`
- Registered: `2026-08-13T22:12:00+07:00`
- Baseline main SHA: `ae9a5fd8cdd6abb903c1c8f8394cae7bfeb97ab0`
- Priority: evidence-driven remote-safe health/readiness integrity

## Reason

`HealthSummary(IEnumerable<ModelHealthIssue>)` currently materializes its caller-controlled issue sequence with an unbounded `ToList()`. This shared terminal aggregation boundary is used by the normal Model Health, aggregate health, release-readiness, rebar, Curtain and mesh health command paths. A non-terminating or excessively large lazy provider can therefore hang or consume memory without limit before a complete readiness object exists. Existing coverage guards null entries, undefined severity and ordinary readiness semantics, but not bounded or single-pass enumeration.

## Reserved scope

- `src/QS3D.Core/Diagnostics/HealthSummary.cs`
- new `tests/QS3D.Core.SmokeTests/HealthSummaryBoundedInputSmoke.cs`
- `scripts/preflight-health-release-readiness.py` only for the minimum static tokens required to pin the new bounded contract
- this claim file

`tests/QS3D.Core.SmokeTests/HealthSummaryReadinessSmoke.cs` remains reserved by another active claim and is explicitly excluded. No command surface, diagnostic-summary exporter, BricsCAD adapter/runtime, release/installer, GitHub Actions, P10, `#987`, `#1005`, or LOCAL-only surface is reserved.

## Intended contract

- Expose an explicit public maximum issue count consistent with the bounded diagnostic-summary policy.
- Snapshot the source enumerable exactly once; accept the exact maximum and reject the first item beyond it.
- Construct no partial or misleading readiness object when enumeration is excessive or throws.
- Preserve ordinary empty/Info/Warning/Error readiness, null issue rejection, undefined severity rejection and source-enumerator exception propagation.
- Add deterministic focused regression coverage and the minimum focused static registration only if required.

## Completion condition

Implementation is complete only when the focused health-summary gate, full Core smoke and aggregate source preflight pass on the implementation merge SHA, the implementation PR is merged normally, and this claim is updated to `COMPLETED` with exact merge and validation evidence.
