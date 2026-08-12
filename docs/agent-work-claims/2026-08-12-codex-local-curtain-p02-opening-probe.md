# Work claim — LOCAL-002 Curtain P02 opening-clipping runtime probe

- Status: `COMPLETED`
- Agent: `codex-local-curtain-p02-probe-20260812` (`/root/audit_preflight_latest`)
- Registered: `2026-08-12T10:53:46+07:00`
- Baseline main SHA: `c71fb530730d464e7cbbdaa7548f901ce0a5d3c6`
- Priority: `LOCAL-002 / P0 / P02` — prepare an exact-SHA disposable V25 probe for the BLT-style Curtain panel opening-clipping gap without claiming runtime evidence remotely.

## Reserved scope

Add an automation-only licensed-runtime probe and guarded PowerShell runner for the existing `LOCAL-002` P02 LINE Curtain behavior. A repository-generated disposable drawing copy will seed two legacy/no-Level GlassWalls: one linked Door must both fully cover at least one source panel cell and partially clip another, while one linked WallOpening must cover the complete clear-panel field and exercise the existing explicit healthy zero-piece build state. The probe will reconstruct the authoritative Core opening plan, compare every non-empty native `Solid3d` panel extent with one planned positive fragment, prove zero positive-area native overlap with the linked opening rectangles, and validate metadata, ownership, live Health and one generated-panel Locate result.

This reservation prepares reusable source/runner/static evidence only. It does not run BricsCAD or promote P02/LOCAL-002 to `LOCAL_PASS`.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/CurtainPanelOpeningRuntimeProbeCommands.cs` — new automation-only prepare/probe commands and privacy-safe aggregate marker.
- `scripts/test-bricscad-v25-curtain-panel-openings.ps1` — new exact-clean-SHA, disposable-copy, outside-repository-artifact runner with process/script/environment/sidecar cleanup proof.
- `scripts/preflight-curtain-panel-opening-runtime-probe.py` — new static contract/privacy/cleanup gate.
- `docs/CURTAIN-NATIVE-PANELS.md` — P02 runner readiness and exact partial/full-cover evidence boundary only.
- `docs/LOCAL-AGENT-INBOX.md` — bounded LOCAL-002 P02 handoff update only.
- this claim for close-out.

Production Curtain planners/builders, Direct Draw commands, Health services and Level placement source are read-only dependencies unless the future licensed runtime run exposes a concrete reproducible defect and this claim is expanded on `main` first.

## Exact evidence contract

- Use only a clean exact Git SHA, repository x64 Release V25 plugin, initialized nonblank BricsCAD profile, ordinary fresh `*.curtain-opening-probe-copy.dwg`, empty artifact directory outside the repository, no pre-existing `.qsdb`/`.bak`, and no pre-existing BricsCAD process.
- Seed two horizontal positive-X LINE GlassWalls in WCS/model space with no Bottom/Top Level properties. Link exactly one Door to the partial scenario and one WallOpening to the complete-empty scenario through existing Direct Draw/Auto Host commands.
- Partial scenario: at least one source panel cell has zero remaining area, at least one has positive reduced area, every emitted piece has finite positive width/height/area, metadata count/area match the authoritative plan, every live owned `Solid3d` extent uniquely matches one planned piece, and positive-area intersection with the linked opening rectangle is zero.
- Complete-empty scenario: all source panel cells are fully covered; build state is `Complete`, opening-aware mode/count and base-grid metadata are valid, panel count is zero, handle metadata is empty, remaining area is zero, and Core/live/runtime panel Health remains non-blocking.
- Across both scenarios, source/opening/host/frame/panel ownership is canonical and disjoint; one non-empty generated panel resolves through Locate to its one GlassWall owner. Marker/metadata contain aggregate counts and hashes only, never raw Handles, element IDs, drawing/plugin paths, layer/text/Family names or private fixture content.
- Stop only the launched BricsCAD PID, delete the generated private `.scr`, restore process environment variables, prove no process/sidecar remains, and prove the disposable DWG SHA-256 is unchanged before publishing PASS metadata.

## Excluded scope

- No edits to `CurtainWallOpeningPanelPlanner`, Curtain host/frame/panel builders, opening topology/clipping formulas, ownership/XData, Direct Draw/Auto Host production commands, generated Health semantics or project persistence.
- No Level assignment/vertical-placement implementation. The probe is legacy/no-Level only and does not overlap active `LOCAL-003`, which owns native Level consumption.
- No overlap with the active Curtain Frame mode-canonicality claim; its Core diagnostic file and semantics are untouched. P02 will consume the final integrated Health behavior only after re-pinning the runtime SHA.
- No P03-P12 qualification, injected rollback, Undo/Redo, save/reopen, multi-DWG, private/customer/BLT fixture or proprietary API/binary access.
- No BricsCAD launch, GitHub Actions dispatch, release, signing, package publication or `LOCAL_PASS` in this source-preparation batch.

## Validation plan

- Re-fetch current `main` and active claims after this claim-only PR merges and before implementation.
- Parse the PowerShell runner and run the new focused Python preflight plus existing Curtain native-panel/runtime/source gates.
- Build the V25 `Release|x64` adapter against the installed managed references without launching BricsCAD.
- Run repository static/source checks proportionate to this additive probe and `git diff --check`; document any unrelated moving-main aggregate blocker truthfully.
- Review final diff for aggregate-only marker fields, exact-SHA/clean-tree enforcement, artifact isolation, launched-PID-only cleanup and no production builder/topology edits.
- Commit/push through a normal PR, rebase/reapply on moving `main` without force push, and do not dispatch Actions.

## Coordination

The active `LOCAL-003` claim explicitly excludes Curtain topology/P01-P12 and owns only shared Level placement consumption; this probe uses legacy/no-Level inputs and does not edit its surfaces. The active Curtain Frame mode-canonicality claim owns only `GeneratedCurtainFrameHealthService` plus its Core smoke. Current active Door/Opening reporting work is completed or owns reporting-only Core surfaces, not Direct Draw/native clipping. If a newer claim reserves any expected file or the same P02 runtime scenario after this registration, stop and reconcile ownership before implementation.

## Completion condition

The claim-only reservation is visible on `origin/main`; the additive P02 probe/runner/static gate and bounded LOCAL-002 docs are merged on current `main`; PowerShell parse, focused static gates and V25 compile pass without a BricsCAD launch; cleanup/privacy/exact-SHA contracts are guarded; the claim is `COMPLETED`; and P02 plus overall LOCAL-002 remain explicitly `PENDING_LOCAL` until a separate licensed exact-SHA run records sanitized evidence.

## Close-out — 2026-08-12

- Claim-only PR: `#789`, squash merge `e4515b9ad9c46b4e1f4e325028db9809eb2ef645`.
- Implementation delivery: branch commit `96f5d3a025ff3de60874415bdeb811f7e55230c8`, PR `#815`; this close-out is part of the same implementation PR and the final squash SHA is assigned by merge.
- Implemented only the five reserved probe/runner/static-gate/runbook/inbox surfaces. Production Curtain planners/builders/topology, generated Health semantics and `LOCAL-003` Level-owned source were not edited.
- Validation PASS after rebase: PowerShell parser; `scripts/preflight-curtain-panel-opening-runtime-probe.py`; Curtain native-panel/orchestration/P01-runtime/runtime-health gates; Level-Curtain placement; Direct Draw openings/P1; `scripts/preflight.py`; and V25 `Release|x64` adapter build with zero warnings/errors against the installed managed references.
- Aggregate discovery was also executed. The P02 gate passed; the aggregate remained red only on pre-existing moving-main gates outside this diff. A clean `origin/main@38c6244e43630cb2837f419a5da51cc7b545c2c1` comparison failed 66 gates while the pre-rebase P02 branch failed 65, so no aggregate regression was attributed to this lane.
- No BricsCAD process, GitHub Actions workflow, private/customer/BLT fixture, release, signing or package publication was used. Runner readiness is `REMOTE_DONE`; P02 and overall LOCAL-002 remain `PENDING_LOCAL` and `production_local002_qualified=false` until a separate clean exact-main-SHA licensed V25 run records sanitized evidence.
