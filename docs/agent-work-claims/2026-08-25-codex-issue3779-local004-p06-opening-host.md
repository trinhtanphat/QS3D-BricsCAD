# Work claim — LOCAL-004/P06 Door/Opening host native-edit lifecycle

- Status: `LOCAL_PASS / BOUNDED`
- Lane-Key: `issue-3779`
- Issue: #3779
- Source defect: #3787 (fixed by PR #3791)
- Parents: #80 / LOCAL-004 and #72
- Branch: `agent/codex/issue3779-local004-p06-opening-host`
- Registration baseline: `origin/main@f4f3fb4fcdb553d76419f9cec86c92a56acb2ba6`
- Exact source-fix retest: `849339a0f7c5f1ef9659320117c5134f53740d11`
- Predecessor failing source: `3c5367e9408c5f93b58163e493ae95c0bbaf206c`
- Host gate: licensed BricsCAD V25.2.10 Windows x64

## Reserved bounded row

Qualify one repository-generated disposable Door/WallOpening host workflow beyond the already-PASS LOCAL-004 P01-P05 cells:

1. production-author one wall host and one uniquely linked Door/WallOpening source;
2. materialize the owned host solid and physical opening-cut state;
3. perform a real native edit of the authoritative source and prove old generated output is unchanged before synchronization;
4. run production `QS3DSYNCSOURCE`, require dependency-closure invalidation without cross-owner mutation, then rebuild the host/opening-cut state through production commands;
5. verify native geometry, semantic metrics/relations, physical-cut provenance, generated ownership and scoped Health;
6. exercise applicable native Undo/Redo, explicit save, graceful close/cold reopen and second-DWG refusal/isolation boundaries;
7. restore DemandLoad/private state, remove disposable files and leave zero test-owned processes.

Exact Git SHA, plugin/Core ProductVersion, plugin/Core SHA-256 and PDB SourceLink must be pinned before licensed execution. Raw handles, ProjectIds, fingerprints, paths and runtime logs remain ignored; only sanitized booleans/counts/classifications may be published.

## Implementation boundary

This is a LOCAL_ONLY runtime-qualification lane. It may use an ignored private probe to select, mutate and inspect disposable native state, but it does not own production adapter/Core changes. A reproducible product defect stops the bounded run and returns the smallest sanitized classification to a separate remote/source child. No private/customer DWG, release/signing action, manual workflow dispatch or `main` merge is authorized.

## Exact-source and baseline receipt

- The registration commit and upstream both resolved to exact tested SHA `3c5367e9408c5f93b58163e493ae95c0bbaf206c`. The worktree was clean; plugin/Core ProductVersion was `0.1.0-preview.10081`; both PDB SourceLink payloads bound that exact SHA.
- The repository submodule was initialized at its pinned commit before the official rerun. The first missing-submodule build attempt is a harness false-start, not a product verdict.
- The official baseline rerun passed all `1020/1020` automated gates, Core Release and deterministic smoke, V25 `Release|x64` with zero warnings/errors, offline WPF, and licensed exact-candidate `NETLOAD` / Ribbon / Palette runtime smoke.
- Exact V25 plugin/Core SHA-256 values were `1dd25d3564efe2a17ca9ba32122804c777dad817d94c4b786d6047a856e1473e` / `cb6325e880bda429effb35c2c4c80bb60bcd2abe04f2e69eb019b55b062be542`.

## Licensed P06 result

The final ignored verifier drove only production authoring/mutation commands plus a read-only state classifier:

1. production Wall + linked Door authoring, host rebuild and selected physical cut;
2. real top-level native crossing-window `STRETCH` from 5 m to 8 m;
3. proof that semantic/cut output stayed at the 5 m state before sync and that physical-cut live Health reported the expected stale-input condition;
4. production `QS3DSYNCSOURCE`, complete old host/cut invalidation, production rebuild and selected recut;
5. native marked Undo of that recut.

Every pre-Undo boundary passed: `baseline_cut_verified`, `native_stretch_verified`, `pre_sync_output_isolation_verified`, `pre_sync_stale_health_verified`, `source_reconcile_verified`, `dependent_invalidation_verified`, `dependent_rebuild_verified` and `physical_recut_verified` were all `true`. Native Undo restored the Solid3d to its uncut volume (`undo_native_geometry_class=UNCUT_RESTORED`) while the host retained the complete post-cut semantic metadata (`undo_semantic_metadata_class=CUT_STATE_RETAINED`). The bounded verdict is therefore:

```text
LOCAL_FAIL / SOURCE_FIX_REQUIRED
failure_code=NATIVE_UNDO_SEMANTIC_DIVERGENCE
undo_coherent=false
redo_status=NOT_RUN_DUE_UNDO_FAILURE
production_local004_qualified=false
```

Issue #3787 owns the source-safe correction. `OpeningBooleanService` commits the boolean and `PhysicalOpeningCut*`/audit state without a document-bound semantic before/after transition enrolled in either existing native Undo coordinator. This local lane made no production source change and must not resume Redo, save/cold reopen or second-DWG qualification until #3787 publishes a new exact pushed fix candidate.

Two earlier ignored P06 attempts were harness-only false-starts: the first allowed an implied pickset to make native `STRETCH` reinterpret its crossing token, and the second placed Door selection before `UNDO Mark`, which cleared PICKFIRST. Prompt-history evidence isolated both before the corrected final run. Neither attempt published a product marker.

The final runner isolated DemandLoad `2 -> 0 -> 2`, preserved the installed loader path and bytes, left zero BricsCAD processes, did not save or create a QSDB sidecar, and the disposable DWG remained byte-identical to repository fixture SHA-256 `cec1350fb2207542aeecd96a790a198a6c9cc9e99a9f875871f367554b3d967e`. The host required an exact-PID forced stop only after the atomic FAIL marker was already published; no save/cold-reopen claim is made. Raw paths, handles, ProjectIds, fingerprints, scripts and screenshots remain Git-ignored.

## Source-fix exact-SHA requalification

Remote/source PR #3791 merged the #3787 correction at
`92b7182437885ceee6b95876fa9bd57adb22dc57`. The local carrier then merged
then-current `origin/main@2c5e62a066829a82d930bd83233fd1028c30e5c1`, producing clean pushed exact
candidate `849339a0f7c5f1ef9659320117c5134f53740d11`; both source-ready and current-main
commits are ancestors. The pinned `external/QS3D-Platform` commit was
`a5778f4abcf3b5c308c5d6854040dbc0c3082390`. Exact plugin/Core SHA-256 values
were `241632FA643C111814981F4C81F1436DFA14CE477DAF0B9138B643847419E6D5` and
`73CA1842FDB09E93784A8A38D0FD26F8A1BAF956C1BC8DC9A87CCF3E2EA3AB43`.
After runtime evidence, `origin/main` advanced to
`443dfe93ca471541604787508ba039049ccb9f41` through unrelated Core persistence
changes; the carrier was refreshed after this docs commit, while the relevant
opening/Undo/project-context source stayed byte-identical and the exact tested
SHA remained an ancestor.

The official exact-candidate baseline passed all `1023/1023` aggregate feature
gates, Core Release with zero warnings/errors, deterministic Core smoke
(`ALL PASS`), V25 `Release|x64` with zero warnings/errors, offline WPF and
licensed V25.2.10 x64 exact-candidate NETLOAD/Ribbon/Palette/native-host identity.

The source-fix rerun then passed the complete bounded P06 workflow:

1. production Wall + linked Door authoring, host materialization and selected
   physical cut;
2. real top-level crossing-window native `STRETCH` from 5 m to 8 m, with old
   semantic/generated output unchanged before production `QS3DSYNCSOURCE`;
3. expected stale physical-cut Health before sync, complete dependency
   invalidation, production host rebuild and selected recut;
4. marked native Undo restored the uncut Solid3d and exact pre-cut semantic
   state (`UNCUT_RESTORED` / `NO_CUT_STATE`);
5. a fresh grouped recut followed by directly adjacent native `U` -> `REDO`
   restored both cut Solid3d and exact post-cut semantic state
   (`CUT_RESTORED` / `CUT_STATE_RESTORED`);
6. explicit `QS3DSAVE` plus native `QSAVE` left project pending false and
   `DBMOD=0`, then the first V25 process closed gracefully;
7. a fresh V25 process cold-reopened the saved DWG/QSDB and verified the 8 m
   source/semantic state, host relation, physical-cut provenance/live geometry
   and zero physical-cut Health issues;
8. a second disposable DWG with one native LINE ran production
   `QS3DCUTSELECTEDOPENINGS`, refused without creating project/sidecar or
   changing ModelSpace/`DBMOD`, then closed without save; reactivation of the
   primary retained the exact project/cut/Health state.

Both official runtime sessions exited gracefully. The cold-reopen session left
the saved primary DWG and QSDB byte-identical to their post-save hashes and the
secondary copy byte-identical to repository fixture SHA-256
`CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E` with no
secondary sidecar. DemandLoad returned `2 -> 0 -> 2` for every session, the
installed loader SHA-256 remained
`0D89D8D828BCE5CFC966EC2EF54358DC50E4FED560D5A908F94643AFA1D74E30`, and zero
BricsCAD processes remained.

Two source-fix setup attempts are excluded from the official verdict. The first
placed Door PICKFIRST before `UNDO Begin`, which cleared selection and left the
production recut waiting for input; trace proved the preceding native Undo
boundary already passed. The first cold-reopen attempt imposed an out-of-scope
requirement that PICKFIRST remain populated after a non-project refusal; the
production command safely consumed it while project/sidecar/CAD/DBMOD stayed
unchanged. The final probe accepts only empty selection or the original LINE and
rejects any other target. Both excluded attempts restored fixture, DemandLoad,
loader and process state and carry no product verdict.

No production source, tracked test or tracked runner was changed by this local
lane. The bounded verdict is now `LOCAL_PASS`; raw scripts, markers, DWG/QSDB and
machine paths remain ignored.

## Non-overlap

- Do not rerun or relabel LOCAL-004 P01-P05, LOCAL-002 Curtain stale/rebuild P05, LOCAL-017/018, #1744, #3613 or H.1 P07.
- Do not modify the existing Source Reconcile, opening Boolean, generated-geometry or Undo coordinators in this local lane.
- Keep #80 and #72 open; P06 does not qualify the still-broader LOCAL-004 topology/category/dependent/failure matrix or customer release.
- Do not rerun P01-P06 without a material source change that invalidates their exact-SHA evidence.
