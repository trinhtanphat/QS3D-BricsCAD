# Work claim — viewport zoom padding finite validation

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-viewport-padding-finite`
- Registered: `2026-08-12`
- Baseline main SHA: `d98fa91a1582a484710ac7dad6d19f52e3c9ff69`
- Priority: `ViewportCommands.TryZoomSelection` validates finite positive width/height before applying its 1.25 zoom padding, but the multiplication itself can overflow a finite span to Infinity immediately before `SetCurrentView`.

## Reserved scope

- `src/QS3D.BricsCAD.V25/ViewportCommands.cs`: make padded zoom width/height fail closed unless finite and positive before assigning the current view.
- `scripts/preflight-viewport-zoom.py`: lock the post-padding finite-validation contract without weakening the existing WCS-to-DCS framing checks.
- This claim file for close-out.

## Excluded scope

- No camera direction/target/twist changes.
- No Layout, PaperSpace, MLeader, TableStyle, release automation, build publication, or BricsCAD runtime qualification.
- Do not modify the just-completed helper-visibility reconciliation except where needed to assert the new production invariant.

## Evidence

Current `TryZoomSelection` computes and validates `width`/`height`, then assigns `view.Width = width * 1.25d` and `view.Height = height * 1.25d` without validating those padded values. Finite IEEE-754 doubles can overflow during that multiplication.

## Completion condition

Production rejects non-finite/non-positive padded dimensions before mutating the view; the viewport preflight locks that contract; exact implementation evidence is recorded here and the claim is released.
