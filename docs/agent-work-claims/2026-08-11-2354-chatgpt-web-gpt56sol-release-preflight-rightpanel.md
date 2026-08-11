# Work claim — release preflight RightPanel label repair

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-11T23:54:00+07:00`
- Baseline main SHA: `633191a5472dc16bd67c1f7790cf5de02ba5afe3`
- Priority: Owner-requested repair for failed QS3D Cloud V25 Preview Build & Release #23 preflight.

## Reserved scope

Repair the current `scripts/preflight.py` failure caused by the misleading Xref scale label in `RightPanel.xaml`, while verifying that the CAD semantic-rescan stale metric/metadata contract already present on current `main` is preserved. Do not weaken the preflight guard.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/RightPanel.xaml`
- `src/QS3D.BricsCAD.V25/Services/SemanticCaptureService.cs` (read/verification only unless current main regresses before implementation)
- `scripts/preflight.py` (read/validation only)
- this work claim

## Excluded scope

- Xref command behavior or code-behind changes
- unrelated UI redesign
- semantic-capture lifecycle/transaction changes beyond the exact stale source-derived metric/metadata preflight contract
- workflow/release dispatch or publication

## Validation plan

- Re-read current `main` immediately before the implementation write.
- Confirm `RightPanel.xaml` no longer contains the forbidden `Header="Tỉ lệ"` / `Content="Xóa"` labels.
- Confirm `SemanticCaptureService.cs` still contains `ReplaceSourceMetric`, `element.Properties.Remove(key)`, and `StartsWith("CAD.")` required by `scripts/preflight.py`.
- Confirm the implementation commit is on current `main` and report its SHA.

## Coordination

Owner explicitly requested immediate fix and push to `main` for release preparation. Recent-main inspection shows no neighboring claim/commit naming this exact RightPanel preflight lane; source-safe patch is intentionally limited to the failing label unless current main changes the evidence.

## Completion condition

A current-main commit replaces the misleading RightPanel scale header with an unambiguous Xref scale label, preserves the already-correct semantic rescan contract, and this claim is marked `COMPLETED` with the pushed implementation SHA.
