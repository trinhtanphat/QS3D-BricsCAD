# Work claim — Domain Hub responsive footer

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-domain-hub-responsive-footer-20260813`
- Registered: `2026-08-13T17:42:00+07:00`
- Baseline main SHA: `af910adb05f66f22198dd38c38397312723fa755`
- Priority: P1 UI reliability follow-up. `DomainHubWindow` still uses a footer `DockPanel` where the flexible status text and the right runtime-gate label compete for width. Under a narrow BricsCAD-hosted window the left horizontal stack can retain its desired width and starve/clamp the right label instead of giving the status text an explicit shrinkable column. This is the same bounded responsive-layout defect family already fixed in other V25 hub/status surfaces.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml`
- `scripts/preflight-domain-hub-responsive-footer.py` (new focused source regression)

## Intended change

Replace only the Domain Hub footer docking layout with a deterministic grid: indicator in an auto column, status text in a flexible star column with ellipsis, and the runtime-gate label in a right auto column. Preserve existing text, colors, command wiring, business behavior, and product-boundary messaging.

## Excluded scope

- any Domain Hub command/business/CAD behavior
- header/body redesign or other windows
- V26
- GitHub Actions dispatch
- native BricsCAD runtime qualification claims

## Validation plan

- Add a focused offline XAML preflight that parses `DomainHubWindow.xaml` and requires the responsive auto/star/auto footer contract while preserving the status/runtime-gate text.
- Re-fetch the exact pushed XAML and regression source.
- Verify commit ancestry against moving `main` after each write.
- Source/static validation only; no native V25 runtime PASS will be claimed.

## Coordination

Repository search found no current Domain Hub responsive-footer commit/lane. Search-index results for literal `ACTIVE` / `BLOCKED` claim status were empty, and this scope is intentionally disjoint from current Source Reconcile, Curtain, runtime/NETLOAD, Project Tools, closed-polyline, and ribbon-startup lanes.

## Completion condition

The focused footer fix and regression are on `main`, exact source/ancestry are verified, and this claim is marked `COMPLETED` with validation boundaries recorded.
