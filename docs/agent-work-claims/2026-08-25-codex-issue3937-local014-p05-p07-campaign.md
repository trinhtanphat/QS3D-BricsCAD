# LOCAL-014 P05-P07 full interactive remainder campaign

- Status: `PARTIAL_LOCAL_PASS / SOURCE_FIX_REQUIRED`
- Carrier issue: `#3937`
- Source defect: `#3966`
- Parent queue: `#72` / `LOCAL-014`
- Agent: Local Agent D (`agent/local004`)
- Runtime source SHA: `dffb7e334f997a981a9d918c23b67592e232d61f`
- Documentation baseline after runtime: `origin/main@273f172e1c86a3b3de97ce0a61dc1bbbab035b04`
- Evidence branch: `agent/local004/issue3937-local014-full-campaign`
- Date: 2026-08-25

## Exact runtime identity

- Licensed host: BricsCAD V25.2.10 Windows x64.
- Plugin ProductVersion: `0.1.0-preview.10081`.
- Plugin SHA-256: `36B99FACCF98C2AF9048DB12230077747492D26136D0160B8C760499100CEAF2`.
- Core SHA-256: `331C4E2CF9FCD3815E543BAADC91534A8C40588BAC9F04D5E366DD73E47AFC49`.
- Plugin PDB SHA-256: `78C295A72A6F732B8539B87F46B57388C1F6D7F7CB2F3888F42BB4B773330A91`.
- Core PDB SHA-256: `D6016BCB595F807383E37161F389D8D42A79F028AB7BEFA7D0610D7CD79284A0`.
- Repository fixture SHA-256: `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`.
- Exact Plugin/Core portable-PDB source identity and private probe builds: PASS.

The licensed result remains bound to `dffb7e3...`. After runtime finished, this documentation carrier refreshed to later `origin/main`; that synchronization does not relabel the tested binary as a later-SHA runtime run.

## Accepted predecessor evidence

This campaign preserved the already accepted bounded rows instead of rerunning them only for activity:

| Phase | Exact source | Result | Boundary |
| --- | --- | --- | --- |
| P01 | `21ca2d08427013f3ef8154708fef85fb2454ff8f` | `LOCAL_PASS` | Automation quick-positive, fallback Family values, two LINE sources. |
| P02 | `7f57130470d4440f25dd27ea0bc3207cbb777a07` | `LOCAL_PASS` | Real quick aliases, LINE + open POLYLINE, preferred Family and unrelated dirty preservation. |
| P03 | `1736ae8db0086041f0b1e8ce4b79839469b10061` | `LOCAL_PASS` | Physical ESC at Thickness, Height and BottomOffset independently; no mutation. |
| P04 | `f70c694d94f26bb1cc1be8025931d9a1a6559bb0` | `LOCAL_PASS` | Same-ObjectId endpoint edit, planar UCS, INSUNITS, project appearance and same-project version drift fail closed. |

Detailed P03/P04 claims remain in the matching `#3820` and `#3825` work-claim documents. No prior bounded PASS was promoted beyond its stated exact SHA or scenario.

## P05 — remaining preview-to-commit freshness

Five fresh exact-PID V25 cycles exercised production `QS3DCONVERT2DADV` on disposable non-customer drawings. Four intentional drift cases were applied after source review and before commit:

1. Model/Paper-space context drift;
2. non-planar UCS;
3. selected source deletion plus recreation with a new identity;
4. replacement of the reviewed canonical project.

Each production command returned with one source LINE, zero Solid3d and zero production-attributable semantic/audit mutation. The intentional harness drift remained observable until verification, then cleanup restored baseline.

The separate active-DWG switch attempt reached the production numeric prompt, but licensed BricsCAD refused the switch with `BRX_E334_EDOCUMENTSWITCHDISABLED`. Physical ESC ended the command; the source DWG and secondary DWG remained unchanged. This cell is `HOST_BLOCKED`, not a product failure or a fabricated pass.

P05 aggregate result: `PASS` for the exercisable freshness remainder, with exact source identity, five DemandLoad isolation/restoration cycles, graceful host exits, unchanged protected fixture and no disposable/private residue. Sanitized private-probe SHA-256: `306ED09383C918F8A26670A1BB5407881CA42BA49A4509422A82C78E99C49146`.

## P06 — mixed batch and ownership-scoped compensation

Three independent fresh processes exercised real production commands against one LINE plus one open straight POLYLINE while seeding unrelated dirty semantic state, one pre-existing owned solid, one foreign unmarked solid and a second open drawing.

### Successful quick alias

Production `QS3DPLAN2WALLS` produced exactly two current-batch owned Solid3d outputs and two semantic walls. It preserved both sources, unrelated dirty semantics, pre-existing owned/foreign CAD and the second DWG.

### Injected native failure

Production `QS3DCONVERT2D` received a deterministic exception during append of the second Solid3d, after the first current-batch owned output existed. The command ended and verification proved:

- current-batch owned live output count returned to zero;
- both injected output identities were absent;
- source geometry and native baseline digest restored exactly;
- semantic project digest, scalar values, audit, `ChangeVersion`, dirty and pending state restored exactly;
- the pre-existing owned solid, foreign unmarked solid, unrelated dirty semantic state and second DWG survived.

This is direct licensed evidence that compensation removed only output owned by the failing batch and that semantic rollback was exact.

### Successful advanced batch

Production `QS3DCONVERT2DADV` accepted `0.27 m / 3.8 m / 0.2 m`, produced exactly two current-batch owned outputs from the mixed sources and preserved all protected state above.

All three phases restored their native/semantic baselines during cleanup, removed only disposable/private files, restored installed DemandLoad and left zero host processes. Sanitized P06 probe SHA-256: `D547591FBB3A20D58FA5044C4B57FA27EBF2D61BAE672F54C90BBED3915764B6`.

## P07 — Undo defect; QSAVE and cold reopen pass

The first fresh V25 session prepared the P02 mixed-source state, then executed this real production sequence:

1. `QS3DCONVERT2D` created the first wall outside the native Undo group.
2. A native Undo group was opened.
3. `QS3DPLAN2WALLS` created the second wall from the open POLYLINE.
4. The native Undo group was closed.
5. The successful state was verified as exactly two semantic walls and two owned generated solids.
6. Native `U`, then native `REDO`, then native `QSAVE` were executed.
7. A second fresh V25 process reopened the saved drawing and sidecar cold.

### Observed native Undo failure

Native `U` should have removed only the second grouped conversion and restored a coherent `1 semantic wall / 1 owned solid` state. Instead, both semantic walls and both owned solids remained live (`2 / 2`). Sanitized failure code: `UNDO_AFTER_GENERATED_STILL_PRESENT`.

The subsequent native `REDO` introduced no divergence and retained the exact `2 / 2` state, but that no-op path does not qualify the required Undo/Redo transition. Source issue `#3966` owns the correction.

### Persistence controls that passed

- native `QSAVE` completed;
- sidecar persisted (`5674` bytes);
- saved DWG SHA-256: `1B8DB616A69FF991F4F11FC53E3449BA37C21C2F33B88618E0EA181A268F67607`;
- saved sidecar SHA-256: `B7E8388D6A19CE41CEA16EFC79E88B77B9885DB71288CF7486D2807BDBA0DB343`;
- fresh-process cold reopen returned the same coherent two-wall/two-generated state;
- unrelated dirty semantic state remained preserved.

P07 probe SHA-256: `A15D5B6777234B3104A72E2DF2F9AEBBD0E609090D873068B628DFBE599B75BE`.

## Isolation, cleanup and evidence privacy

- Every accepted runtime result used a fresh exact launched PID and repository-generated disposable DWG copies only.
- Another DWG was verified untouched in the applicable P05/P06 cases.
- DemandLoad was isolated only for the active process and restored to installed `LoadCtrls=2` after every cycle.
- The P07 pair exited with one expected session-one discard dialog and no session-two discard dialog.
- All generated scripts, disposable DWGs, sidecars, backups and drawing/project locks were removed.
- Final `bricscad.exe` count was zero and remained stable before the single-host reservation was released with literal `HOST_RELEASED`.
- Raw markers, local probe source/binaries and machine-specific paths remain Git-ignored under `artifacts/`; no private/customer drawing, ProjectId or raw Handle list is committed.
- Earlier harness/setup attempts are `NO_RESULT` only and are not counted as product evidence.
- The source-safe workflow guard now accepts LOCAL-014 only when it remains pending or carries the complete `#3966` blocked-source evidence/follow-up tokens; it still rejects `Status: PASS`. No production implementation source was changed.
- No GitHub Actions were dispatched and no production source file was changed in this local lane.

## Disposition and resume condition

P05 and P06 are bounded `LOCAL_PASS` at exact source `dffb7e3...`; P07 QSAVE/sidecar/cold-reopen controls pass at that same identity. Overall LOCAL-014 remains unqualified because the required native Undo transition failed. `production_local014_qualified=false` is authoritative.

Issue `#3966` must add the smallest coherent Plan-to-3D native/semantic Undo/Redo integration plus source-safe guards. After that fix merges, rerun only the bounded P07 production sequence on a new exact clean intended SHA. Promote LOCAL-014 only if Undo restores `1 / 1`, Redo restores exact `2 / 2`, save/cold reopen persist the current coherent state, isolation holds and cleanup remains complete.
