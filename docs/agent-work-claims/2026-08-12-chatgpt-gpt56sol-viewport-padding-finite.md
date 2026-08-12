# Work claim — viewport zoom padding finite validation

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-viewport-padding-finite`
- Registered: `2026-08-12`
- Baseline main SHA: `d98fa91a1582a484710ac7dad6d19f52e3c9ff69`
- Claim commit: `343773c857112e1ef6d0f6af4a26643cff64f1ba`
- Source fix: `a0b20af96ad5ff163ee08d4b102c5aee41c43269`
- Regression gate: `8a4ce7d10b62fb687af89f5fe5616c437438f3d3`
- Priority: `ViewportCommands.TryZoomSelection` validated finite positive width/height before applying its 1.25 zoom padding, but the multiplication itself could overflow a finite span to Infinity immediately before `SetCurrentView`.

## Completed scope

- `src/QS3D.BricsCAD.V25/ViewportCommands.cs` now computes `paddedWidth` / `paddedHeight`, rejects either unless `FinitePositive`, and only then assigns the current view.
- `scripts/preflight-viewport-zoom.py` now requires the padded values, requires their finite-positive guard to occur before view mutation, and rejects direct unvalidated `width * 1.25d` / `height * 1.25d` assignment.
- Existing WCS-to-DCS framing, camera preservation, command ownership, and TILEMODE-aware model-space assertions were preserved.

## Validation performed

- Re-fetched both production source and viewport preflight after the claim before writing.
- Read back `ViewportCommands.cs` from `main` after the source commit and confirmed the post-padding finite guard precedes `view.Width` / `view.Height` mutation.
- Read back `scripts/preflight-viewport-zoom.py` from `main` after the gate commit and confirmed the ordering/direct-assignment regression checks are present.
- Compared source commit `a0b20af96ad5ff163ee08d4b102c5aee41c43269` to moving `main`: source commit remained the merge base / ancestor.
- Compared gate commit `8a4ce7d10b62fb687af89f5fe5616c437438f3d3` to moving `main`: gate commit remained the merge base / ancestor.
- No force push or overwrite was used while concurrent agents advanced `main`.
- No local Python preflight, local compile, GitHub Actions dispatch, or BricsCAD V25/V26 runtime PASS is claimed in this lane.

## Excluded scope

- No camera direction/target/twist changes.
- No Layout, PaperSpace, MLeader, TableStyle, release automation, build publication, or BricsCAD runtime qualification.

## Completion condition

Completed. Production rejects non-finite/non-positive padded dimensions before mutating the view; the viewport preflight locks that contract; this reservation is released.
