# Agent work claim — issue #2355

- Status: ACTIVE
- Lane-Key: issue-2355
- Owner: ChatGPT GPT-5.6 Sol
- Issue: https://github.com/trinhtanphat/QS3D-BricsCAD/issues/2355
- Branch: `agent/chatgpt-gpt56sol/issue-2355-bim-shell-activation`
- PR: pending
- Original base at registration: `main@89a0f3450caab447365a3ee449712f13b81a96ad`
- Renewed owner-reference baseline: `main@ee3f8dc55d6cd01fd481b7b7c11e425fb09135ef`

## Scope

Repair the real BricsCAD-hosted QS3D `MÔ HÌNH BIM` workspace so it follows the owner-supplied BLT3D reference **without replacing BricsCAD UI or modelspace**.

Default BIM layout target:

1. left narrow column: QS3D Zone/Floor/model tree;
2. adjacent left column: QS3D Family list with the existing authoritative QS3D Properties editor embedded below it;
3. center: native BricsCAD modelspace viewport (no fake/second viewport);
4. right: QS3D drawing/layer management palette;
5. dedicated QS3D Properties and Quantity Insight remain available as isolated/on-demand capabilities but do not auto-open in the default owner-reference BIM layout.

The qualified `Vẽ / Công cụ / IFC` Ribbon surface remains. BricsCAD title/Ribbon host, command/status UI and viewport ownership remain intact.

## Expected touched areas

- `src/QS3D.BricsCAD.V25/PaletteCoordinator.cs`
- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.Blt3dFiveZoneRuntimeLayout.cs`
- directly affected focused source guards under `scripts/`
- `docs/LOCAL-AGENT-INBOX.md` only if the owner-reference local visual scenario must be materially updated

## Overlap / ownership check

- This is the already-open canonical #2355 lane; do not create a duplicate issue/branch.
- #2285/#2396/#2399 and their integration PRs are historical landed implementations. Current `main` is implementation truth.
- PR #2008 is already merged historical Ribbon work; the current lane does not reopen or replace its `Vẽ / Công cụ / IFC` mirror contract.
- The owner supplied a newer reference screenshot that narrows the remaining defect to default BIM workspace visibility/composition and side-by-side left layout.

## Integration contract

- Reconcile this same canonical branch non-force to current `main` before mutation.
- Exact branch-head watched-path CI must complete GREEN before opening the PR.
- Re-sync with latest `main` if it advances before PR creation.
- Fresh PR required checks `preflight` and `core` must be GREEN before merge.
- No force-push, no branch-protection bypass, no direct write to `main`.
- A real full-screen BricsCAD screenshot is LOCAL_ONLY unless produced by an actual licensed Windows/BricsCAD harness.