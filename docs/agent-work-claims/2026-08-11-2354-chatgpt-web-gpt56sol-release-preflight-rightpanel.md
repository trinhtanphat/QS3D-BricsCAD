# Work claim — release preflight RightPanel label repair

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-11T23:54:00+07:00`
- Baseline main SHA: `633191a5472dc16bd67c1f7790cf5de02ba5afe3`
- Priority: Owner-requested repair for failed QS3D Cloud V25 Preview Build & Release #23 preflight.

## Reserved scope

Repair the current `scripts/preflight.py` failure caused by the misleading Xref scale label in `RightPanel.xaml`, while verifying that the CAD semantic-rescan stale metric/metadata contract already present on current `main` is preserved. Do not weaken the preflight guard.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/RightPanel.xaml`
- `src/QS3D.BricsCAD.V25/Services/SemanticCaptureService.cs` (read/verification only)
- `scripts/preflight.py` (read/validation only)
- this work claim

## Excluded scope

- Xref command behavior or code-behind changes
- unrelated UI redesign
- semantic-capture lifecycle/transaction changes beyond the exact stale source-derived metric/metadata preflight contract
- workflow/release dispatch or publication

## Validation executed

- Implementation commit: `ecd6d87101216f7fcd05b4b6318877ff87dcf9c5` (`fix(ui): clarify Xref scale label for release preflight`).
- Verified the implementation commit remains an ancestor of current `main` after concurrent pushes.
- Verified current `RightPanel.xaml` uses `Header="Tỉ lệ Xref"`; the forbidden exact `Header="Tỉ lệ"` token is gone and no `Content="Xóa"` was introduced.
- Verified current `SemanticCaptureService.cs` still clears stale `CAD.*` metadata via `StartsWith("CAD.")` + `element.Properties.Remove(key)` and replaces/removes stale source metrics through `ReplaceSourceMetric`.
- Verified `scripts/preflight.py` still enforces both guards; the guard itself was not weakened.
- Attempted a clean local clone to execute `python scripts/preflight.py`, but this session container could not resolve `github.com`; therefore no full local preflight PASS is claimed.
- No GitHub Actions workflow was dispatched or re-run by this task.

## Coordination

Owner explicitly requested immediate fix and push to `main` for release preparation. The patch was intentionally limited to the failing RightPanel label because the second run-#23 failure was already fixed by current-main semantic-capture code.

## Completion condition

Completed: the source defect behind the remaining current-main RightPanel preflight failure is fixed on `main`, the already-correct semantic rescan contract is preserved, and the implementation SHA is recorded above.
