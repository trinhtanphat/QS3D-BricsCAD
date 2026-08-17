# Agent work claim — issue #2355

- Status: ACTIVE
- Lane-Key: issue-2355
- Owner: ChatGPT GPT-5.6 Sol
- Issue: https://github.com/trinhtanphat/QS3D-BricsCAD/issues/2355
- Branch: `agent/chatgpt-gpt56sol/issue-2355-bim-shell-activation`
- PR: pending
- Base at registration: `main@89a0f3450caab447365a3ee449712f13b81a96ad`

## Scope

Repair the real BricsCAD-hosted QS3D BIM workspace so activation renders the intended BLT3D-inspired five-region contract without turning QS3D into a standalone shell:

1. QS3D Workspace menu/tree / zone / floor.
2. QS3D family and properties region in the Workspace palette.
3. Native BricsCAD modelspace viewport as the center 3D canvas.
4. QS3D drawing/layer management palette.
5. QS3D quantity explanation/detail palette.

Trace and fix BIM ribbon/workspace activation and lifecycle re-assertion, ensure the Workspace reference shell is actually applied, preserve ordinary `PaletteCoordinator.Show()` isolation, and add regression guards for the activation path and hosted-product boundary.

## Expected touched areas

- `src/QS3D.BricsCAD.V25/Infrastructure/PaletteCoordinator.cs`
- `src/QS3D.BricsCAD.V25/Ribbon/**`
- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel*.cs`
- related regression/preflight scripts only as required

## Overlap / ownership check

- Prior issue #2285 / PR #2313 is completed and merged; this lane addresses the remaining live activation/rendering defect exposed by the current BricsCAD screenshot.
- No open exact-overlap PR/issue was found before registration.
- Owner explicitly requested this lane to continue and finish the fix.

## Integration contract

- Exact branch-head watched-path CI must complete GREEN before opening the PR.
- Re-sync with latest `main` if it advances before PR creation.
- Fresh PR required checks `preflight` and `core` must be GREEN before merge.
- No force-push, no branch-protection bypass, no direct write to `main`.
- A real full-screen BricsCAD screenshot is LOCAL_ONLY unless produced by an actual licensed Windows/BricsCAD harness.