# Work claim — LOCAL-004/P06 Door/Opening host native-edit lifecycle

- Status: `LOCAL_FAIL / SOURCE_FIX_REQUIRED`
- Lane-Key: `issue-3779`
- Issue: #3779
- Source defect: #3787
- Parents: #80 / LOCAL-004 and #72
- Branch: `agent/codex/issue3779-local004-p06-opening-host`
- Registration baseline: `origin/main@f4f3fb4fcdb553d76419f9cec86c92a56acb2ba6`
- Exact tested source: `3c5367e9408c5f93b58163e493ae95c0bbaf206c`
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

## Non-overlap

- Do not rerun or relabel LOCAL-004 P01-P05, LOCAL-002 Curtain stale/rebuild P05, LOCAL-017/018, #1744, #3613 or H.1 P07.
- Do not modify the existing Source Reconcile, opening Boolean, generated-geometry or Undo coordinators in this local lane.
- Keep #80 and #72 open; this row cannot qualify overall LOCAL-004 or customer release.
- Do not rerun unchanged `3c5367e...`; wait for #3787 to publish a distinct exact source-fix SHA, then start with the same marked recut Undo boundary before expanding to Redo/save/reopen/multi-DWG.
