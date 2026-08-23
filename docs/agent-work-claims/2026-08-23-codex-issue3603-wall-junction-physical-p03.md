# LOCAL-007 P03 — physical wall-junction materialization

Status: `COMPLETED / LOCAL_PASS`

Parent: #73 / LOCAL-007

Implementation and qualification issue: #3603

Lane-Key: `issue-local007-p03`

Task branch: `agent/codex/issue3603-v25-wall-junction-physical-p03`

Exact pushed runtime candidate: `4a546ae7d5fcf2e47f0d2670858fad25abde4e83`

Runtime `origin/main` baseline: `ddbe528157a29656647ee7da0fcb8b441f512016`

## Boundary

This local-only P03 cell implements and licenses the BricsCAD V25 native boundary for the existing CAD-independent `WallJunctionOwnershipPlanner`. It creates dedicated multi-owner junction solids, persists strict ownership, replaces complete owner groups, exposes read-only Health, and invalidates dependent output when a participating wall changes. It does not change V26, Wall Snap Preview/Apply, AutoCAD adapters, customer drawings, releases, installers or unrelated local queues.

The runtime used only disposable public-fixture copies plus one independent blank DWG created by BricsCAD for the two-document isolation cell. The private probe, runner, raw command logs, drawing copies, sidecars, handles, identities and nonces remain Git-ignored under `artifacts/`. No customer/private drawing, proprietary binary or machine-specific evidence path is committed.

## Implementation delivered

- `QS3DWALLJUNCTION3D` accepts eligible semantic LINE/open-POLYLINE wall sources, excludes generated QS3D output and expands a selected owner to the complete same-plane project topology.
- Each physical occurrence receives a dedicated vertical cylindrical `Solid3d`, centered in the shared owner elevation range with radius `MinThicknessM / 2`; it does not boolean, consume or relabel a participating wall host.
- Versioned `QS3D_WALL_JUNCTION` XData records hashed project/drawing identity plus exact `WJP1:` group, `WJX1:` occurrence owner, `WJF1:` input fingerprint and complete owner/source dependency sets.
- Destructive work validates all junction records first. Replacement, stale cleanup and owner add/remove operate on the complete `GroupToken`; corrupt, ambiguous, duplicate or foreign ownership fails closed.
- Wall rebuild/reconcile invalidation, generated-source exclusion, dedicated read-only Health, Health All, Release Check, Locate and Ribbon/Start Center/Workspace reachability are wired without assigning junction output to one semantic wall.
- `scripts/preflight-wall-junction-native-materialization.py` locks the native source contract, including the centered-frustum elevation rule discovered during licensed geometry inspection.

## 2026-08-23 exact-SHA licensed result

`LOCAL_PASS / BOUNDED_P03_PHYSICAL` was recorded on the exact clean pushed candidate above with BricsCAD V25.2.10 x64. Plugin/Core ProductVersion was `0.1.0-preview.10081`; SHA-256 values were `1541EABCBA6C5445BEF0652F18EE6DB6C790E85E0A0C3FE2756A7DCCA8E6932C` and `7311C5008E78C9FFF9842E12243AB397E845156139D497EDEBBB4110EA080080`. PDB SourceLink for both assemblies matched the exact candidate SHA.

Three isolated BricsCAD sessions exited gracefully with code `0` and returned 61/61 allowlisted PASS markers:

- Topology/geometry: L, T, X and bounded Multi nodes passed with 2, 3, 4 and 5 semantic owners. One two-owner group produced two deterministic occurrences. Native bounds, positive mass volume, planned shared bottom/top and minimum-thickness radius matched independently; mixed thickness and nonzero owner elevations were covered.
- Replacement: selected-only-owner planning expanded to the full same-plane group. Exact-current output retained its native handle. Profile and source changes replaced only the complete affected group; owner add/remove moved T to X and back while removing the stale former group. A generated junction selected as input was rejected without mutation.
- Host safety: the original wall-generated solid kept its handle/volume. A hosted Door kept the same `HostWallId` and semantic dependency throughout junction creation and rebuild.
- Health/repair: deleting one owned occurrence produced the exact missing/incomplete Health findings; rebuilding restored the group and returned Health to zero.
- Failure safety: incompatible vertical ranges produced no output before mutation. Duplicate owner tokens, a corrupt marker version and a foreign project identity each blocked production replacement and surfaced the expected Health code while preserving the valid existing output; controlled cleanup restored Health to zero.
- Native lifecycle: BricsCAD Undo removed only the command-owned new group, and Redo restored it with the same native handle while pre-existing groups remained stable.
- Persistence: production QS3D save plus DWG save, close and cold reopen preserved ownership identities, native handle and zero Health; a repeated build was idempotent. A second independently created DWG in the same BricsCAD process received distinct project/drawing identities and one healthy output; returning to the primary drawing preserved its original identity, handle and Health.

## Repository validation and safety

- `QS3D.BricsCAD.V25` Release|x64 against the licensed V25 installation: `0 warnings / 0 errors`.
- Private runtime probe Release build: `0 warnings / 0 errors`.
- Nine focused Wall Junction and Health preflights: PASS.
- Aggregate source preflight discovered 990 gates: 989 PASS; the sole failure was unchanged `scripts/preflight-plan-to-3d-finish-workflow.py`, which reports LOCAL-008 `Evidence: PENDING_LOCAL` outside this diff.
- Public fixture SHA-256 remained `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`. The user's protected drawing was never opened and its bytes remained unchanged.
- Fixed per-user V25 DemandLoad loader bytes and `LoadCtrls=2` were preserved. The runner did not write `SECURELOAD` or `TRUSTEDPATHS`.
- All sessions ended with zero residual BricsCAD processes. Raw/private evidence and disposable data remain outside the tracked diff.

## Remaining scope

Issue #3603 closes this bounded physical-output P03 cell after its task-branch PR is integrated. Parent #73 and overall LOCAL-007 remain open: Wall Snap P02 is still `LOCAL_PARTIAL / PENDING_REMOTE` until #3600 and #3601 land and the successful Apply/invalidation/replacement-no-cache matrix is rerun on an exact pushed SHA.
