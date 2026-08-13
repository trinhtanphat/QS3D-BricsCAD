# Work claim — V25 status DockPanel ordering gate

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-v25-status-dock-order-gate-20260813`
- Registered: `2026-08-13T18:26:00+07:00`
- Completed: `2026-08-13T18:29:00+07:00`
- Baseline main SHA: `7cf118157e8ca7189fad0400428ee9c92ee77e27`
- Priority: P1 regression closure for the responsive status/footer defect family. Multiple V25 windows required the same bounded repair because a status/totals TextBlock appeared before a final `DockPanel.Dock="Right"` hint while `LastChildFill` remained enabled.

## Reserved scope

- `scripts/preflight-v25-status-dock-order.py`
- this claim file

## Result

- Dynamic regression gate: `f3d742f91ec8145936931cede8b8019128391bf8` (`test(ui): gate V25 status DockPanel ordering`).
- The gate scans every top-level V25 UI XAML file except `Theme.xaml`, parses every `DockPanel`, and rejects only the proven stale anti-pattern: default/enabled `LastChildFill`, final direct child marked `DockPanel.Dock="Right"`, and an earlier direct status/totals/summary-like TextBlock (`x:Name` contains status/total/summary or `Text` binds Status).
- Correct right-before-final-fill DockPanels remain allowed. `LastChildFill="False"` layouts remain allowed. Grid-based responsive status rows are unaffected.
- Violations are aggregated so one run reports every affected XAML surface instead of stopping at the first one.
- No production XAML/C# source changed under this regression-only lane.

## Validation actually executed

- Re-fetched the exact pushed gate source `3672e0f9f979480648edacf41c1d88e563f71967` and reviewed the dynamic XAML iteration, `LastChildFill` boundary, final-right-child condition, status-like matching, and aggregated error reporting.
- `python -m py_compile` on the exact gate text reconstructed from the pushed source in an isolated fixture exited `0`.
- Positive fixture with the right-docked badge before final status fill: PASS.
- Positive fixture with status before a right-docked final child but explicit `LastChildFill="False"`: PASS, proving the gate does not ban legitimate explicit docking.
- Negative fixture with `TotalsText` before a final right-docked hint: expected FAIL and named both `TotalsText` and the hint.
- Negative fixture with unnamed `{Binding Status}` before a final right-docked Border: expected FAIL, proving binding-based status detection.
- `compare_commits(f3d742f91ec8145936931cede8b8019128391bf8, c8d8718fcc3ad78cf2a3032574fafb6782489990)` returned `status=ahead`, `behind_by=0`, merge-base equal to the gate commit. The only newer file at closeout was an unrelated MAP-02 signed-zero claim.
- A full repository execution of this new gate was not performed in a materialized checkout during this lane, so the claim is limited to exact source verification plus focused detection-boundary fixtures. No GitHub Actions or native BricsCAD runtime validation was dispatched.

## Coordination

Recent commit search found no existing responsive-footer/DockPanel ordering gate. This lane reserved no production XAML and remained disjoint from concurrent MAP-02/Core and V25 runtime/startup work.

## Completion condition

Satisfied for the regression-only scope: the dynamic gate is on `main`, exact source and ancestry were verified, focused positive/negative fixtures validate its intended boundary, and the claim is closed without production changes.
