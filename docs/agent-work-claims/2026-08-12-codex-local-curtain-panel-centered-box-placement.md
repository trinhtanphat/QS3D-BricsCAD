# Work claim — LOCAL-002/P02 Curtain Panel centered-box placement

- Status: `COMPLETED`
- Agent: `codex-local-curtain-panel-centered-box-placement-20260812` (`/root/audit_preflight_latest`)
- Registered: `2026-08-12T11:52:35+07:00`
- Baseline main SHA: `488fc84811f75e7ee435dcfb7f6ef3ce6851bc8e`
- Priority: `LOCAL-002 / P0 / P02` — correct the production native Curtain Panel placement defect isolated by the licensed opening-clipping probe.

## Confirmed defect and evidence

The V2 P02 runner was executed on clean exact main SHA `7c160de66de68c811282f4cd460e927370e454cd` after a zero-warning/error installed-reference V25 build. It returned only the sanitized `failure_phase=DOOR_NATIVE_GEOMETRY` and `failure_code=STATE_REJECTED`. Cleanup was independently verified: source-copy hash preserved, no BricsCAD process, private script, sidecar, backup or metadata remained.

The phase boundary proves the Door source shape, authoritative opening plan, production output ownership, metadata and planned geometry all passed before native extent matching failed. Source inspection supplies the deterministic cause: `CurtainWallPanelBuilderSupport.CreateBox(...)` calls V25 `Solid3d.CreateBox(width, depth, height)`, then applies `Displacement(-width/2,-depth/2,-height/2)`, rotates and translates to a target that is already the intended panel center. Installed V25 behavior has already been established by LOCAL-003 and LOCAL-014: `Solid3d.CreateBox` is centered at creation. The extra displacement therefore moves each native panel center by negative half extents before rotation; for the positive-X P02 LINE it produces planned/native min-X and min-Z offsets of negative half width/height while leaving width/height unchanged, so no native piece can match its authoritative plan.

This is a production Curtain Panel placement bug. It is not an opening-clipping formula, probe measurement or metadata defect.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Cad/CurtainWallPanelBuilderSupport.cs` — remove only the duplicate negative half-dimension displacement inside the shared LINE/path panel `CreateBox(...)` helper; preserve the existing centered target, rotation, transactions, ownership, clipping plan, XData and Level/base-Z inputs.
- `scripts/preflight-curtain-panel-opening-runtime-probe.py` — require create → rotate → target-center placement and forbid the duplicate negative half-dimension displacement.
- `src/QS3D.BricsCAD.V25/CurtainPanelOpeningRuntimeProbeCommands.cs` only if a static explanatory token or unchanged matching contract needs alignment; no measurement weakening or tolerance relaxation.
- `docs/CURTAIN-NATIVE-PANELS.md` and bounded `LOCAL-002` inbox text only to record the isolated defect/fix and rerun requirement.
- this claim for close-out.

No Curtain frame builder is reserved. Similar-looking frame placement remains out of scope because this P02 evidence measured panel output only. Any frame correction requires its own runtime evidence and published claim.

## Intended contract

- `Solid3d.CreateBox(width, depth, height)` remains centered; rotate that centered solid about origin, then translate it once to the already computed panel center.
- For the horizontal positive-X synthetic P02 wall, native min-X/min-Z/width/height must equal each authoritative positive panel piece within the existing strict tolerance; no panel may intersect the linked opening.
- Shared LINE and path panel placement use the same corrected helper, but this lane qualifies only P02 LINE Door/complete-empty behavior.
- Do not change authoritative panel planning, opening clearance, area/count metadata, native matching tolerance, ownership/replacement, Health or Level semantics.

## Validation plan

- Merge this claim-only reservation before any production edit and re-fetch active claims.
- Run the focused P02/static privacy gate, Curtain native/orchestration/P01-runtime/runtime-health gates, Level-Curtain and Direct Draw opening gates.
- Build V25 `Release|x64` against installed managed references without launching BricsCAD; run `scripts/preflight.py`, PowerShell parse and `git diff --check`.
- Deliver through normal PR/squash merge without force-push or Actions. A local agent then rebuilds the final clean exact-main SHA and reruns P02 once on a fresh disposable copy/empty external artifact directory.

## Excluded scope

- No edits to Curtain planners, frame builders, host builders, Direct Draw, generated Health/ownership/XData, Level placement, persistence or P03-P12.
- No change to probe bounds/tolerance/matching that could hide physical misplacement.
- No BricsCAD run in this source batch, private/customer/BLT fixture, Actions, release, signing or package publication.
- No P02/LOCAL-002 promotion until the complete exact-SHA runtime PASS marker and cleanup invariants succeed.

## Coordination

The active parent-owned P02 runtime-qualification claim explicitly kept production source read-only pending a separate evidence-backed expansion; this claim is that bounded expansion. Active Curtain division and panel-area Health claims own Core planner/Health plus smokes only and do not overlap this adapter helper/gate. LOCAL-003 owns Level consumption, not panel topology/placement correction; its previous runtime evidence is consumed only to establish V25 centered-box behavior.

## Completion condition

The claim is visible on `origin/main`; the single production helper correction and static regression are merged; focused gates and V25 build pass without BricsCAD; the claim is `COMPLETED`; and one exact clean merged-main SHA is handed back for the P02 licensed rerun. Expected PASS requires all existing partial/full-cover/native-match/zero-intersection/Health/Locate/cleanup assertions; any FAIL must remain allowlisted and P02/LOCAL-002 remain `PENDING_LOCAL`.

## Close-out

- Claim-only reservation: PR `#845`, squash merge `26e757fd829e858546b42ca4f18ea6424730e403`.
- Source implementation: branch commit `ea99304a77beb232fc1ca27c71a37135b1d4942d`, PR `#850`, squash merge `37003ee721760159d630326bfb90cb602f5abbdb`.
- Correction: removed only the duplicate negative half-extent displacement from `CurtainWallPanelBuilderSupport.CreateBox(...)`; the existing centered-box rotation and target-center displacement remain unchanged. No frame, planner, opening, tolerance, metadata, Health or Level source was modified.
- Static regression: the P02 gate now requires create -> rotate -> target-center placement in that helper and forbids the duplicate V25 centered-box shift.
- Validation after current-main integration: focused Curtain opening/native/orchestration/P01-runtime/runtime-health, Level-Curtain and Direct Draw gates PASS; PowerShell runner parses; aggregate `scripts/preflight.py` PASS; installed-reference V25 `Release|x64` build succeeds with zero warnings/errors; `git diff --check` PASS.
- No BricsCAD, GitHub Actions, private/customer fixture or private runtime artifact was opened or run in this source batch.
- This closes the source-fix lane only. P02 and overall LOCAL-002 remain `PENDING_LOCAL` until one fresh disposable-copy run built from the exact final clean merged-main SHA returns the complete V2 PASS marker and all cleanup invariants.
