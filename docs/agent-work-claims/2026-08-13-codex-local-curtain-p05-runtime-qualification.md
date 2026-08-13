# Work claim — LOCAL-002/P05 licensed Curtain stale/rebuild qualification

- Status: `ACTIVE`
- Agent: `codex-local-curtain-p05-runtime-qualification-20260813` (`/root`)
- Registered: `2026-08-13T10:45:00+07:00`
- Baseline main SHA: `b4a8cf161f858ec6ade23b0a59281c56b9f69a7d`
- Priority: `LOCAL-002 / P0 / P05` — execute the prepared BLT-style Curtain stale/rebuild state machine in the installed BricsCAD V25 environment.

## Reserved scope

- Run `scripts/preflight-curtain-panel-stale-rebuild-runtime-probe.py` and build the exact repository V25 x64 Release adapter from one clean final `main` SHA.
- Make an ordinary disposable copy of the repository-generated `samples/generated/QS3D-Sample.dwg`, renamed with the required `*.curtain-stale-rebuild-probe-copy.dwg` suffix, in a fresh GUID directory outside the repository and synchronized folders.
- Run `scripts/test-bricscad-v25-curtain-panel-stale-rebuild.ps1` against that copy, the initialized `QS3D-V25-TEST` profile, and a fresh empty outside-repository artifact directory.
- Read only sanitized marker/metadata output. Never publish raw Handles, semantic IDs, profiles, paths, exception details, drawing content, `.scr`, `.dwg` or `.qsdb` data.
- On PASS, update only `docs/CURTAIN-NATIVE-PANELS.md` and this claim with exact SHA, DLL hash, BricsCAD version and aggregate transition/cleanup evidence. On FAIL, publish only allowlisted `failure_phase` / `failure_code` and cleanup booleans; any product fix requires a separately merged claim first.

## Required acceptance

- Baseline plus grid, depth, height, linked-opening and direct-source-edit/reconcile transitions all complete in order.
- Semantic changes show explicit owner stale/config stale before target-only rebuild and clear only after coherent replacement. Direct CAD source edit remains semantically unstale before `QS3DSYNCSOURCE` while live geometry/config Health reports drift.
- Each valid replacement has disjoint old/new target panel sets, no old target panel remains live, native AABBs uniquely match the authoritative plan, counts/grid/depth/height/area/config/live state are coherent, native ownership remains clean and one generated panel Locates to the target owner.
- The unrelated control GlassWall retains its exact source/host/frame/panel ownership, fingerprints, metadata and native bounds throughout.
- Disposable DWG SHA-256 is unchanged, the launched PID exits, the private script is removed and no sidecar/backup remains.

## Exclusions and safety

- No production builder/planner/Health/ownership/Level/SourceReconcile edit is authorized by this claim.
- No private/customer/BLT drawing or spreadsheet, owner original, GitHub Actions, release, signing, packaging or installation change.
- P05 alone may become bounded `LOCAL_PASS`. P06-P12 and overall LOCAL-002 remain `PENDING_LOCAL`; the run does not qualify arbitrary edit ordering, Undo/save-reopen, multi-DWG, Level placement or exact curved glass.

## Completion condition

The claim is already visible on `origin/main` before launch; one clean exact-SHA licensed run returns the complete PASS and cleanup contract; sanitized evidence is merged normally without force-push or Actions; and this claim is marked `COMPLETED` with exact PR/commit identities.
