# Work claim — reporting numeric SourceHandle identity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-reporting-numeric-source-handle-identity`
- Registered: `2026-08-12T20:00:00+07:00`
- Baseline main SHA: `5c2063e5e4f0f6f89e233b98b34ef53f4c22a668`
- Priority: P1 — keep reporting provenance aligned with the shared CAD numeric-handle identity contract.

## Confirmed defect

`ReportingRowProvenance.AppendSourceHandles(...)` currently rejects blank, padded and case-only duplicate stored `SourceHandles`, but it compares identities with `OrdinalIgnoreCase` only. The shared CAD ownership/Locate boundary canonicalizes numeric hexadecimal handles through `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)`, so aliases such as `A`, `0A`, `000A` and `0xA` represent one CAD object.

Concrete counterexample: two valid report members in the same grouped row carry direct stored handles `A` and `0A`. Reporting currently accepts both and emits `QuantityReportRow.SourceHandleText == "A;0A"`, even though Locate/ownership resolve both to the same numeric CAD identity. The row therefore exposes duplicate/ambiguous provenance for one CAD object.

## Reserved scope

- `src/QS3D.Core/Reporting/ReportingRowProvenance.cs`
- Focused Core smoke coverage for reporting numeric SourceHandle identity.
- Minimal smoke registration/preflight adjustment only if required by the existing test harness.

## Intended contract

- Preserve existing fail-closed rejection of blank and surrounding-whitespace `SourceHandles`.
- Compare stored report provenance by the same numeric CAD handle identity used by shared ownership/Locate logic.
- Reject numeric aliases within one source sequence or across members merged into one report row before publishing duplicate provenance.
- Preserve the first stored canonical token for valid unique handles; do not rewrite report/export schema, grouping, quantity math, or unrelated source ownership state.

## Out of scope

- Generated ownership services, `SourceHandleResolver`, CAD runtime lookup, interchange provenance, quantity formulas, report grouping, XLSX/CSV schema, BricsCAD runtime, GitHub Actions, and unrelated handle lanes.
