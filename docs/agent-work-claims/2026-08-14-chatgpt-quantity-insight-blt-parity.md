# Work claim — Quantity Insight BLT parity / click-to-3D

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-quantity-insight-locate-20260814`
- Registered: `2026-08-14T15:41:30+07:00`
- Baseline main SHA: `504ee7c601b103065805da85fbd04da27336735c`
- User evidence: screenshot comparing BLT3D quantity explanation (left) with QS3D Quantity Insight (right); QS3D command line repeatedly reports `QS3D: chưa có đối tượng được chọn để zoom.` while Click = 3D is enabled.

## Claimed scope

- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel*` only for Quantity Insight row/detail selection, existing exact geometry-explainer presentation, and click-to-3D locate behavior.
- `src/QS3D.BricsCAD.V25/Cad/CadHandleService.cs` only if a shared live-handle selection defect is proven necessary for this Quantity Insight lane.
- `src/QS3D.BricsCAD.V25/Services/SourceHandleResolver.cs` only if the current semantic-to-live-CAD resolution is proven to be the cause of the Quantity Insight locate miss.
- One focused regression/preflight surface for the above behavior if required.

## Explicit exclusions

- No changes to Core quantity arithmetic, intersection/deduction rules, quantity mapping/category policy, BREP geometry computation contracts, Quantity Summary, Wall Quantity, Project Browser/Grid work, releases/signing, or GitHub Actions policy.
- No speculative changes to unrelated active claims.
- Existing BREP-exact explainer computation is reused; this lane is presentation/wiring/locate parity only.

## Plan

1. Re-read current `main` after this claim lands and verify no newer overlapping claim/source commit.
2. Trace Quantity Insight row click → detail selection → handle resolution → implied selection → `QS3DZOOMSELECTED`.
3. Fix the zero-selection path without weakening canonical/stale-data guards, and make the already-implemented exact quantity explanation visible/usable at the selected component level so the QS3D workflow exposes gross, deductions, net, and formwork detail comparable to the supplied BLT3D reference.
4. Add focused regression coverage, commit/push to `main`, then update this same claim to `COMPLETED` with exact SHA and validation evidence.

Completion requires source read-back on current `main`; native BricsCAD runtime acceptance remains distinct unless explicitly run on a licensed V25 host.