# Work claim — Model Health baseline bounded issue enumeration

- Status: `ACTIVE`
- Agent: `Codex / GPT-5`
- Registered: `2026-08-13T22:23:00+07:00`
- Baseline main SHA: `21b6f0a2ff24555cef9bdcdd36f1830727018343`
- Priority: evidence-driven remote-safe preview/health-guard integrity

## Reason

`ModelHealthBaselineService.Capture(ProjectState, IEnumerable<ModelHealthIssue>)` passes caller-controlled issue input through `Unique(...)` to `Index(...)`, which enumerates it without a bound. A non-terminating or excessively large provider can therefore hang or consume memory without limit before a complete baseline exists. This baseline is the health regression authority used by guarded Quantity Rule and Regeneration Preview flows. Existing coverage protects null/undefined issues, deterministic identity, deduplication, sorting, stale semantics and read-only capture, but not bounded or single-pass enumeration.

## Reserved scope

- `src/QS3D.Core/Diagnostics/ModelHealthBaselineService.cs`
- new `tests/QS3D.Core.SmokeTests/ModelHealthBaselineBoundedInputSmoke.cs`
- `scripts/preflight-model-health-baseline.py` for the minimum static tokens needed to pin the bounded contract
- this claim file

Existing baseline smoke files and every caller/preview/rule/command surface are explicitly excluded. No `HealthSummary`, diagnostic-summary exporter, BricsCAD adapter/runtime, release/installer, GitHub Actions, P10, `#987`, `#1005`, or LOCAL-only surface is reserved.

## Intended contract

- Expose an explicit public maximum of `1000000` issues, consistent with the bounded diagnostic policy.
- Snapshot caller input exactly once; accept the exact maximum and reject the first item beyond it.
- Propagate the original source enumeration exception and return no partial baseline on any enumeration failure.
- Preserve existing null/undefined-severity rejection, structural deduplication, deterministic sorting, stale-message identity and comparison behavior.
- Add isolated module-initializer regression coverage without editing existing reserved baseline smoke files.

## Completion condition

Implementation is complete only when the focused Model Health baseline gate, full Core smoke and aggregate source preflight pass on the implementation merge SHA, the implementation PR is merged normally, and this claim is updated to `COMPLETED` with exact merge and validation evidence.
