# Workspace responsive bottom navigation implementation plan

**Issue:** #4147  
**Branch:** `agent/interactive-20260827-wsn7/issue-4147-workspace-responsive-nav`

## Goal

Remove the `QS3D — MÔ HÌNH` palette's horizontal overflow at the layout source, keep the existing three-zone workspace usable at narrow/normal palette widths, and replace the legacy three-button footer with a fixed five-item bottom navigation surface.

## Task 1 — RED responsive shell contract

Add `scripts/preflight-workspace-responsive-bottom-nav.py` before production code. The guard must fail while the responsive shell is absent and require the final source to:

- force `WorkspaceOverflow` horizontal scrolling off;
- clear the hard root minimum width;
- convert the three content columns to proportional star widths with zero minimums;
- keep the two splitters fixed;
- install a 42 px footer navigation row;
- expose `Mô hình`, `Cấu kiện`, `Hoàn thiện`, `Thống kê`, and `⋯`;
- keep model-health and refresh actions reachable from `⋯`.

## Task 2 — Responsive three-zone layout

Add a focused `WorkspacePanel` partial that applies the layout correction without rewriting the large existing XAML surface or competing with BLT3D runtime repair code.

- Apply the correction from initialization and again on size changes.
- Set the workspace root minimum width to zero.
- Disable horizontal panning/scrollbar fallback.
- Use proportional browser / family-workspace / properties columns so the center absorbs remaining width.
- Preserve fixed splitter widths and all existing pane content.

## Task 3 — Fixed bottom navigation

Replace the existing footer child at runtime with a five-column navigation grid in the existing bottom row.

- `Mô hình` preserves the existing 3D-view action.
- `Cấu kiện` focuses the Family / Type pane.
- `Hoàn thiện` focuses the model/category tree.
- `Thống kê` preserves the existing quantity action.
- `⋯` opens an upward menu with `Kiểm tra mô hình` and `Làm mới`.
- Reuse the current dark-theme resources and existing command handlers.

## Task 4 — Verification and handoff

Run/observe the focused guard and the repository branch CI on the exact head. Review the final diff for unrelated changes. Open a PR linked to #4147. Hosted/static verification must not be reported as licensed BricsCAD `LOCAL_PASS`; any host visual qualification remains a separate local-only step.
