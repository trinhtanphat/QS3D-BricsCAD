# Work claim — V25 status DockPanel ordering gate

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-v25-status-dock-order-gate-20260813`
- Registered: `2026-08-13T18:26:00+07:00`
- Baseline main SHA: `7cf118157e8ca7189fad0400428ee9c92ee77e27`
- Priority: P1 regression closure for the responsive status/footer defect family. Multiple V25 windows have required the same bounded repair because a status/totals TextBlock was placed before a final `DockPanel.Dock="Right"` hint while `LastChildFill` remained enabled. In WPF that final child fills the remaining DockPanel area instead of behaving as an ordinary right-docked sibling, leaving the status/totals row width-dependent.

## Reserved scope

- `scripts/preflight-v25-status-dock-order.py` (new dynamic source regression)
- this claim file

## Intended change

Add a dynamic offline XAML gate that scans all `src/QS3D.BricsCAD.V25/UI/*.xaml` DockPanels and rejects the specific stale anti-pattern only when all of these are true: `LastChildFill` is enabled/default, the final direct child declares `DockPanel.Dock="Right"`, and an earlier direct TextBlock is status/totals/summary-like (`x:Name` contains status/total/summary or binds Status). Correct DockPanels where the right-docked badge appears before the final fill text remain allowed, as do `LastChildFill="False"` panels and grid-based responsive rows.

## Excluded scope

- production XAML/C# changes
- general DockPanel redesign or blanket ban on right docking
- business/domain/CAD logic, V26
- GitHub Actions dispatch and native BricsCAD runtime claims

## Validation plan

- Parse every V25 UI XAML with `xml.etree.ElementTree`; report all violating file/status-name pairs in one run.
- Focused fixtures must prove: stale status-then-final-right layout fails; correct right-before-final-status passes; `LastChildFill="False"` passes; responsive Grid rows are unaffected.
- Re-fetch exact pushed gate and verify ancestry against moving `main`.
- No native runtime PASS will be claimed.

## Coordination

Recent commit search found no existing responsive-footer/DockPanel ordering gate. This regression-only lane does not reserve any production XAML currently owned by other agents and intentionally encodes only the repeatedly proven status/footer ordering invariant rather than speculative layout rules.

## Completion condition

The dynamic gate is pushed to current `main`, focused positive/negative fixtures validate the intended detection boundary, exact source/ancestry are verified, and this claim is marked `COMPLETED`.
