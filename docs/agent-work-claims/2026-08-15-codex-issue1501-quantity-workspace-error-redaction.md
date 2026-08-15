# Work claim — Quantity workspace callback error redaction

- Status: `ACTIVE`
- Agent: `codex-root-20260815`
- Registered: `2026-08-15T10:15:00+07:00`
- Baseline main SHA: `992ef63f1049bb6b49f2cf5b598e908757137eb1`
- Follow-up: completed issue `#1501` / merged PR `#1504`
- Priority: repair a review-blocking disclosure merged into the BLT-inspired quantity workspace

## Confirmed defect

`QuantitySummaryWindow.EstimateWorkspace.cs` catches a modeless command-dispatch exception and reflects raw `Exception.Message` text into both shared palette status and a message box. This contradicts the existing Quantity Summary callback containment contract, but `preflight-quantity-summary-callback-error-containment.py` currently scans only `QuantitySummaryWindow.xaml.cs`, so the newly added partial escaped the guard.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.EstimateWorkspace.cs`: use stable local failure text without raw exception reflection.
- `scripts/preflight-quantity-summary-callback-error-containment.py`: cover all `QuantitySummaryWindow*.cs` partials and pin the new stable callback text.
- this claim and exact validation/closeout evidence.

## Exclusions

No Workspace quick-action behavior, command routing, quantity calculations, native geometry, project mutation, other UI callbacks, BricsCAD runtime execution, private data, release/signing, or GitHub Actions changes.

## Validation plan

- claim-only PR merged before implementation edits;
- focused Quantity Summary callback/layout/command gates and generic/manual-only preflight;
- installed-reference V25 `Release|x64` build with `0 warnings / 0 errors`;
- aggregate feature preflight; no BricsCAD launch and no Actions dispatch.
