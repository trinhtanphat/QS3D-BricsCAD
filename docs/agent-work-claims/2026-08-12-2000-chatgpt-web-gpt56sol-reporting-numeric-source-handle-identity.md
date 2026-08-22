# Work claim — reporting numeric SourceHandle identity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-reporting-numeric-source-handle-identity`
- Registered: `2026-08-12T20:00:00+07:00`
- Completed: `2026-08-12T20:29:00+07:00`
- Baseline main SHA: `5c2063e5e4f0f6f89e233b98b34ef53f4c22a668`
- Priority: P1 — keep reporting provenance aligned with the shared CAD numeric-handle identity contract.

## Confirmed defect

`ReportingRowProvenance.AppendSourceHandles(...)` previously rejected blank, padded and case-only duplicate stored `SourceHandles`, but compared identities with `OrdinalIgnoreCase` only. The shared CAD ownership/Locate boundary canonicalizes numeric hexadecimal handles through `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)`, so aliases such as `A`, `0A`, `000A` and `0xA` represent one CAD object.

Concrete counterexample: two valid report members in the same grouped row carry direct stored handles `A` and `0A`. Reporting previously accepted both and could emit `QuantityReportRow.SourceHandleText == "A;0A"`, even though Locate/ownership resolve both to the same numeric CAD identity. The row therefore exposed duplicate/ambiguous provenance for one CAD object.

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

## Closeout

- Claim commit: `3b4a51b34f3f2eb2827b6e7f0f180a47676d8649`.
- Source fix: `695797a55feaba7e0096334963447123105d22d9` — reporting provenance now compares stored handles through `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)` and fails closed on duplicate numeric identities.
- Focused regression: `b0cba1c93b60031bda72d1d49665635994e4fa6c` — `ReportingSourceHandleIdentitySmoke` locks `A`/`0A` rejection across grouped elements and preserves distinct `A`/`B` provenance; the smoke self-registers with `ModuleInitializer`.
- GitHub readback confirmed both the source fix and focused regression on `main` before closeout.
- GitHub Actions/CI and BricsCAD runtime were intentionally not invoked in this lane; no force-push was used.
