# Work claim — LOCAL-014/P02 Plan-to-3D quick aliases and preferred Family

- Status: `COMPLETED`
- Agent: `codex-plan2d-p02-local-prep` (`/root/plan2d_p02_local_prep`, remote/source preparation only)
- Registered: `2026-08-14T10:36:29+07:00`
- Baseline main SHA: `cc8c866b5186aad96ec842e8921813f6448bcd0d`
- Priority: `LOCAL-014 / P1 / P02` — prepare the smallest high-value licensed follow-up after bounded P01 quick-positive evidence.

## Reserved scope

Add one automation-only, privacy-safe P02 preparation lane for the existing production Plan-to-3D quick workflow. A guarded disposable-copy probe will exercise both `QS3DCONVERT2D` and its `QS3DPLAN2WALLS` alias against one LINE and one open straight POLYLINE, use an explicitly active/preferred `ArchitecturalWall` Family with non-fallback numeric defaults, and prove an unrelated dirty semantic element retains the same dirty flags.

This is source preparation only. No BricsCAD process will be launched and no runtime PASS will be recorded in this claim.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/PlanTo3DP02RuntimeProbeCommands.cs` — additive automation-only prepare/select/verify commands and in-process disposable state.
- `scripts/test-bricscad-v25-plan-to-3d-p02.ps1` — dedicated exact-SHA, zero-pre-existing-process, outside-repository artifact runner for a future local worker.
- `scripts/preflight-plan-to-3d-p02-runtime-probe.py` — focused command/runner/privacy/cleanup/static contract.
- `docs/PLAN-TO-3D-WORKFLOW.md` and `docs/LOCAL-AGENT-INBOX.md` — P02 source-ready handoff while retaining `LOCAL-014 / PENDING_LOCAL`.
- this claim file for integration and closeout.

## Required contract

- Invoke the real production commands through the BricsCAD script sequence; automation may only seed/select supported sources and inspect results.
- Use one LINE for `QS3DCONVERT2D` and one open, zero-bulge, straight POLYLINE for `QS3DPLAN2WALLS`.
- Create and activate one canonical `ArchitecturalWall` Family through current project services, then verify both walls reference that Family and inherit its exact `ThicknessM`, `HeightM`, and `BottomOffsetM` values rather than projectless fallbacks.
- Seed one unrelated semantic element with known dirty flags and verify object identity, category, source provenance, properties, quantities, and dirty flags remain unchanged across both quick conversions.
- Verify source geometry retention, one live owned Solid3d per converted source, disjoint source/output sets, native bounds, wall-scoped Core/runtime Health, and sanitized aggregate marker fields.
- The runner must require Windows interactive mode, clean exact SHA, canonical x64 Release DLL, initialized nonblank profile, a fresh `*.plan-to-3d-p02-probe-copy.dwg`, empty outside-repository artifacts, no pre-existing BricsCAD process/sidecar/backup/lock, unchanged disposable drawing hash, launched-PID exit, private-script deletion and environment restoration.
- Marker/metadata must exclude paths, profiles, Handles, semantic/project/Family IDs or names, layer/text content, exception details, XData and customer data.

## Excluded scope

- No edits to `PlanTo3DCommands.cs`, `WallSolidBuilder`, `PolylineWallSolidBuilder`, semantic capture, Family services, ownership/Health implementations or any other production behavior.
- No `QS3DCONVERT2DADV` prompt cancellation/drift matrix, forced rollback/failure injection, Undo/Redo, save/reopen, multi-DWG or overall `LOCAL-014` promotion.
- No overlap with active `SE` closed-POLYLINE production work; this lane accepts only the existing Plan-to-3D LINE/open-POLYLINE contract and adds no user command.
- No LOCAL-002/P10/P11, LOCAL-003/004, polygon rebar, wall-junction, updater/release, private/customer DWG, BLT binary/API, V26 runtime, installer/signing or GitHub Actions operation.

## Validation plan

- Parse the dedicated runner with Windows PowerShell 5.1 without launching it.
- Run the focused P02 gate plus existing Plan-to-3D runtime/quick/PICKFIRST/lifecycle/source-freshness/scoped-regeneration gates.
- Run `scripts/preflight.py`, aggregate `scripts/preflight-all.py`, Core Release build/smoke, and installed-reference V25 `Release|x64` build without starting BricsCAD.
- Run `git diff --check`, inspect the final exact diff and re-fetch/reconcile current `origin/main` before every publish/merge.
- Do not dispatch GitHub Actions.

## Coordination

The current `ACTIVE` / `BLOCKED` claim and open-PR scans found no reservation for `LOCAL-014/P02`, the three planned additive files, or its bounded runtime scenario. The active Build3D claim owns only `Build3DCommands.cs`; the active `SE` claims own a separate closed-POLYLINE production command and do not reserve Plan-to-3D automation. Existing P01 is `COMPLETED` and explicitly leaves aliases, open POLYLINE, preferred Family and unrelated-dirty proof pending.

## Closeout evidence

- Completed: `2026-08-14T12:20:01+07:00`.
- Claim-only PR `#1119` merged as `f1c1697384357335cf006bbfbfa677b91b6a7a11` before implementation; the published claim was verified on `origin/main`.
- Implementation local head `52265352ce876c5de7b44ce598cca0c427dffd43` was pushed after rebasing onto then-current `origin/main`; PR `#1129` squash-merged as `bfce63c3e68b2731e809c283622cd1f275cf679c`.
- Merged surfaces are limited to the additive automation-only prepare/select/verify commands, guarded runner, focused static/privacy gate, and the two LOCAL handoff docs. `PlanTo3DCommands.cs`, `WallSolidBuilder` and `PolylineWallSolidBuilder` remained unchanged.
- PASS: Windows PowerShell parser; focused P02 and existing Plan-to-3D runtime/quick/PICKFIRST/lifecycle/preview/source-freshness/scoped-regeneration/finish gates; `scripts/preflight.py`; Core Release smoke (`ALL PASS`); installed-reference V25 `Release|x64` build (`0` warnings, `0` errors); `git diff --check`.
- Aggregate `scripts/preflight-all.py` remained baseline-blocked by four unrelated Curtain gates: `preflight-curtain-empty-partition-orchestration.py`, `preflight-curtain-frame-atomicity.py`, `preflight-curtain-frame-transaction-boundary.py`, and `preflight-curtain-orchestration-boundary.py`. This lane did not change Curtain source or gates.
- No BricsCAD process or GitHub Actions was run; no private/customer data was used. P02 is only `SOURCE_READY / PENDING_LOCAL`, and overall LOCAL-014 remains `PENDING_LOCAL` until a clean exact-SHA licensed run produces sanitized evidence.

## Completion condition

The claim is visible on `origin/main`; additive probe/runner/gate/docs are merged after static/build validation; the claim is marked `COMPLETED` with exact SHAs; and `LOCAL-014/P02` remains `SOURCE_READY / PENDING_LOCAL` until a local worker executes the guarded runner on one clean exact merged-main SHA.
