# LOCAL-008 Direct Draw view-preservation acceptance

Status: ACTIVE
Agent: codex-local003-direct-draw-view-20260815
Issues: #72, #74
Branch: `agent/local003/local008-direct-draw-view-20260815`
Baseline: `2504eb2a56ca040cd6c897a60510df4b8e8db89e`
Handoff row: `docs/LOCAL-SHEET-ACCEPTANCE-HANDOFF-2026-08-15.md` row 2

## Reserved scope

- Qualify the existing one-shot `QS3DDRAWBEAM` workflow on interactive Windows x64 with licensed BricsCAD V25.
- In one disposable public fixture copy, establish a non-default view/zoom, create two consecutive Beam elements through two explicit production-command invocations, and capture sanitized before/after viewport measurements.
- Verify each successful command keeps the intended generated/source implied selection, does not force `VPOINT`/`ZOOM` or otherwise jump the camera, and leaves coherent semantic/native ownership.
- Cancel one additional `QS3DDRAWBEAM` invocation and prove view, semantic/native counts and disposable state do not partially change.
- Use only an ignored local script/probe wrapper needed to drive and observe the licensed host. Do not commit that local artifact.
- Publish only bounded row-2 evidence in this claim, `docs/LOCAL-AGENT-INBOX.md` and `docs/LOCAL-SHEET-ACCEPTANCE-HANDOFF-2026-08-15.md`.

## Explicit boundary

- This row does not claim or implement issue #74's still-open true continuous/repeated DrawJig mode, transient thickness/profile preview, document-switch editor lifecycle or broader all-command cancellation matrix.
- No production, Core, adapter, test, runner, workflow or packaging source changes are reserved.
- Any product/source defect returns to a non-local source-fix lane with the smallest sanitized reproduction.
- No private/customer drawing, proprietary binary, screenshot, secret, GitHub Actions dispatch/re-run/cancel, release, direct `main` write, force-push or merge.

## Validation plan

1. Push this claim and draft PR, then verify local/remote head identity before any build or licensed execution.
2. Refetch current `main`; if it moves before qualification, merge it non-force, update/push the exact candidate and only then begin.
3. Confirm interactive Windows x64, licensed BricsCAD V25.2.10 x64, zero pre-existing BricsCAD processes and a clean worktree.
4. Run `preflight-direct-draw-view-preservation.py`, the relevant Direct Draw source guards, manual-only CI policy and local-handoff guard.
5. Build Core smoke and the V25 adapter `Release|x64` sequentially with the portable .NET SDK and installed V25 references; require zero warnings/errors and matching exact-SHA ProductVersion.
6. Create a disposable copy of the public synthetic fixture under ignored `artifacts/`, record the original/copy hash, drive the production command twice plus one cancellation, and collect only aggregate view/selection/ownership/count markers.
7. Require stable camera/zoom measurements within a documented tolerance, two successful distinct owned results, cancel/no-partial-mutation, graceful host exit, unchanged canonical fixture hash, and zero process/script/sidecar/lock residue.
8. Commit/push only sanitized documentation, rerun focused/policy/handoff guards on the pushed evidence head, and stop before merge.

## Expected repository surfaces

- `docs/agent-work-claims/2026-08-15-codex-local008-direct-draw-view.md`
- `docs/LOCAL-AGENT-INBOX.md` (sanitized bounded result only)
- `docs/LOCAL-SHEET-ACCEPTANCE-HANDOFF-2026-08-15.md` (sanitized row-2 result only)
