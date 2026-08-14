# Work claim — Quantity Insight BLT parity / click-to-3D

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-quantity-insight-locate-20260814`
- Registered: `2026-08-14T15:41:30+07:00`
- Completed: `2026-08-14T15:52:30+07:00`
- Baseline main SHA: `504ee7c601b103065805da85fbd04da27336735c`
- Implementation commits:
  - `4027e907f9c115d713b8880074c6c08dab2c5bf7` — tree-row Click=3D / locate now zooms the implied selection synchronously on the bound document.
  - `871a8a8394b6026447c31e13e615dde2ca571a78` — canonical detail locate uses the same exact document-bound zoom path.
  - `8701cca079cf3f7e0f0e07f1bac0524f8957eeb1` — BREP-exact detail is equation-first (`gộp - trừ = còn`), deduction rows remain clickable, and deduction locate zooms synchronously.
  - `ed19f066243102e27e6afe2db7ffbf7bd735983a` — the full component derivation is vertically scrollable instead of being clipped by the detail card height.
- User evidence: screenshot comparing BLT3D quantity explanation (left) with QS3D Quantity Insight (right); QS3D command line repeatedly reported `QS3D: chưa có đối tượng được chọn để zoom.` while Click = 3D was enabled.

## Claimed scope

- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel*` only for Quantity Insight row/detail selection, existing exact geometry-explainer presentation, and click-to-3D locate behavior.
- `src/QS3D.BricsCAD.V25/Cad/CadHandleService.cs` only if a shared live-handle selection defect is proven necessary for this Quantity Insight lane.
- `src/QS3D.BricsCAD.V25/Services/SourceHandleResolver.cs` only if the current semantic-to-live-CAD resolution is proven to be the cause of the Quantity Insight locate miss.
- One focused regression/preflight surface for the above behavior if required.

## Result

- Root cause of the visible `chưa có đối tượng được chọn để zoom` failure was the Quantity Insight UI selecting the correct live handles and then queuing `QS3DZOOMSELECTED` for later command re-entry. That allowed the implied-selection context to disappear before the queued command read it. All three Quantity Insight locate paths now call the repository's existing `ViewportCommands.TryZoomSelection(document)` immediately after `CadHandleService.Select(...)`, preserving the exact document and pickset.
- The existing `QuantityGeometryExplanationService` BREP-exact computation remains the source of truth; no Core quantity arithmetic, intersection/deduction policy, or geometry contracts were changed.
- Component explanation now surfaces the concrete equation first, keeps individual intersection deductions clickable, labels volume/formwork as `GỘP - TRỪ = CÒN`, places exact geometry before generic metrics, and uses one outer vertical scroll so the derivation remains accessible inside the bounded palette card.
- Existing stale-project / canonical-row validation guards remain intact.

## Validation evidence

- Read-back on `main` confirmed the tree-row and component-detail paths call `ViewportCommands.TryZoomSelection(document)` directly after resolving/selecting current handles.
- Read-back on `main` confirmed BREP-exact output now exposes the equation and derivation before generic metrics, with the detail body scrollable.
- GitHub Actions were not dispatched because repository CI is manual-only and the user requested source fix + commit/push, not a CI run.
- Native BricsCAD V25 acceptance is `LOCAL_ONLY`: a licensed V25 host must still smoke-test Click=3D, detail locate, and clickable deduction rows against the user's DWG.

## Explicit exclusions respected

- No changes to Core quantity arithmetic, intersection/deduction rules, quantity mapping/category policy, BREP geometry computation contracts, Quantity Summary, Wall Quantity, Project Browser/Grid work, releases/signing, or GitHub Actions policy.
- No changes were required in `CadHandleService` or `SourceHandleResolver`.
- No speculative changes to unrelated active claims.
