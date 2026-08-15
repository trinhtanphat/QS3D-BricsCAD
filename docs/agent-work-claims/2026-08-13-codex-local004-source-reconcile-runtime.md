# Work claim — LOCAL-004 Source Reconcile native atomicity

- Status: `ACTIVE`
- Agent: `codex-local-root-20260813`
- Registered: `2026-08-13T15:02:00+07:00`
- Baseline main SHA: `bd429d3ceec1058f984fca068ce54aeb88e391fe`
- Priority: `LOCAL-004 / P0` — `QS3DSYNCSOURCE` requires licensed BricsCAD V25 transactions, real source edits, native generated-output invalidation, Undo/Redo, multi-DWG and cold reopen.

## Reserved scope

Prepare and execute one exact-SHA synthetic V25 qualification for the complete current LOCAL-004 scenario. Create tracked LINE/open-POLYLINE semantic sources and owned generated output through production authoring/build commands; edit their live CAD geometry; invoke production `QS3DSYNCSOURCE`; and verify source-derived semantics, dependency regeneration, ownership-scoped native invalidation and explicit rebuild.

The matrix must also select generated output and an intentionally ambiguous source owner to prove fail-closed refusal without mutation; force one post-invalidation/pre-commit failure through an incompatible native `INSUNITS` versus canonical bound unit to prove CAD transaction abort plus semantic snapshot restoration; exercise native Undo/Redo; run an isolated B-document refusal without cross-project mutation; and cold save/reopen the successful A result. Any semantic/native Undo divergence or ordinary production bug is reported with sanitized evidence to a remote source-fix issue instead of being fixed in this local lane.

## Expected surfaces

- new `src/QS3D.BricsCAD.V25/SourceReconcileRuntimeProbeCommands.cs`
- new `scripts/test-bricscad-v25-source-reconcile.ps1`
- new `scripts/preflight-source-reconcile-runtime-probe.py`
- `docs/SOURCE-RECONCILE-GENERATED-OUTPUTS.md` only for guarded runtime handoff/result
- `docs/LOCAL-AGENT-INBOX.md` only for exact LOCAL-004 result
- this claim file

## Excluded scope

- No edits to `SourceReconcileCommands.cs`, `Services/SourceReconcileService.cs`, `Cad/GeneratedDependentGeometryInvalidator.cs`, production builders, unit policy, project persistence, Undo integration or other product behavior.
- No customer/private DWG, BLT/BRC binary inspection, GitHub Actions, release/signing/installer/V26 work, broad LOCAL-001 baseline or LOCAL-003 Level work.
- No overlap with open responsive UI PR #975 or any ACTIVE/BLOCKED remote source claim.

## Validation plan

- Re-fetch `origin/main`, claims, PRs and issues before each commit; claim-only PR must merge before probe implementation.
- Use only ordinary copies of repository-generated `samples/generated/QS3D-Sample.dwg` under a fresh outside-repository artifact root and exact clean-SHA V25 x64 Release DLL/Core pair.
- Capture deterministic semantic/native/source digests around every success/refusal/rollback phase; never publish raw Handles, semantic/project IDs, paths, profiles, drawing contents or exception text.
- Require production command boundaries for Direct Draw, `QS3DSYNCSOURCE`, rebuild, Undo/Redo, `QS3DSAVE`, native save and reopen. Automation may prepare selection/edit/corruption state and inspect results but may not call Source Reconcile internals to manufacture a pass.
- Accept PASS or sanitized allowlisted source-blocking FAIL only after every launched PID exits, private scripts and exact sidecar/backup/lock files are removed, and disposable DWGs are restored byte-for-byte.
- Run the focused runtime gate, existing Source Reconcile gates, generic preflight, installed-reference V25 Release build and licensed runner. Do not dispatch Actions.

## Coordination

The baseline scan found no ACTIVE/BLOCKED claim, open PR or existing runner owning `LOCAL-004`, `QS3DSYNCSOURCE` licensed qualification or the three proposed additive files. Prior Source Reconcile source claims are completed. The active LOCAL-003 claim explicitly excludes LOCAL-004.

## Completion condition

The claim is visible on `origin/main`; the guarded probe/runner/gate are merged; an exact-SHA licensed result covers success, generated/ambiguous refusal, forced rollback, Undo/Redo, multi-DWG and cold reopen; LOCAL-004 evidence is recorded; and the claim is `COMPLETED`. A reproduced production defect leaves LOCAL-004 source-blocked/PENDING with a remote issue and sanitized handoff rather than a false local pass.

## 2026-08-14 exact-SHA rerun evidence

- Clean candidate `f42171d3f9dab336fa8874547a3016271977546d` built against installed BricsCAD V25 references with `0 warnings / 0 errors`; Core smoke returned `ALL PASS`, and the focused Source Reconcile runtime/coherence/preflight gates passed before launch. The exact adapter SHA-256 was `E36D98EC5097339F0D76EBB78CF6C38C7C1F9D78A0C43590E99B50666583F4B8`.
- The guarded BricsCAD `25.2.10` runner returned sanitized `FAIL` at `verify_final_reconcile / SEMANTIC_SOURCE_MISMATCH`. Selection was `BOTH_SOURCES`, owner match was `NONE`, generated state was `RETAINED_ALL`, and project/revision/native marker were all `UNCHANGED`. History was already `DESYNCHRONIZED` before the final command and remained so afterward, with entry class `MULTIPLE` on both sides.
- Process, private script, private state and drawing restoration were all verified clean; launcher handoffs were zero. No source file or runtime harness was changed by the local worker. Sanitized handoff is recorded in GitHub issue `#1005` comment `#issuecomment-5289658766`.
- The claim remains `ACTIVE / PENDING_LOCAL`. The source defect is assigned back to the non-local source-fix lane; no LOCAL-004 acceptance or issue closure is claimed.

## 2026-08-14 source-candidate repeat result

- The exact source candidate requested by issue `#1005`, `032cb6e3923772cf40755fb62fe891ea2a239010`, passed Core smoke, all four focused Source Reconcile gates and the installed-reference V25 `Release|x64` build. Plugin/Core ProductVersion matched the exact SHA; adapter SHA-256 was `DBF90108F6304714A7E0B297D0601E3927BF432B9D7264346C8DE68BA58978B7`.
- The licensed runner again returned `verify_final_reconcile / SEMANTIC_SOURCE_MISMATCH` with the same sanitized tuple: `BOTH_SOURCES`, owner match `NONE`, generated `RETAINED_ALL`, project/revision/native marker all `UNCHANGED`, and history `DESYNCHRONIZED` with `MULTIPLE` entries before and after the final command. Process/script/private-state/drawing cleanup all passed and launcher handoffs were zero.
- The global command-depth/detach suppression therefore did not prevent the pre-final-command desynchronization. Sanitized handoff is issue comment `#issuecomment-5289769355`; no source or runner change was made locally. The claim remains `ACTIVE / PENDING_LOCAL`.

## 2026-08-14 post-Undo marker discriminator split

- After exact candidate `8a5fbb2ca6b406a5ad4776da1b110b4d863af37b` reached `native_undo / NATIVE_UNDO_SEMANTIC_DIVERGENCE` with `undo_coherent=false` and `redo_coherent=true`, `/root` explicitly delegated one narrow automation-only edit to `/root/fix_source_reconcile_desync` under claim `2026-08-14-gpt56sol-issue1005-post-undo-marker-discriminator.md`.
- That successor owns only sanitized post-Undo/post-Redo marker classification in the probe/runner/focused privacy gate. This claim retains exclusive ownership of licensed BricsCAD execution, the complete LOCAL-004 matrix, private/local evidence, cleanup, and result publication. Production Source Reconcile behavior remains outside both automation edits until the discriminator is rerun.

### Discriminator implementation handoff

- The delegated lane implemented the discriminator as additive automation-only `SourceReconcilePostUndoMarkerProbeCommands.cs`, leaving production Undo/history/marker code and the existing main runtime probe behavior untouched.
- The unchanged runner sequence now surrounds final reconcile and guarded native Undo/Redo with private snapshot captures, then publishes only four bounded fields into the existing session-one marker: `post_undo_marker_vs_pre_final_state`, `post_undo_marker_vs_post_final_state`, `post_redo_marker_vs_pre_final_state`, and `post_redo_marker_vs_post_final_state`.
- Each field is allowlisted to `ADVANCED`, `UNCHANGED`, or `MISSING_OR_INVALID`; the runner keeps the sanitized session-one marker in local metadata so the local owner can hand the tuple back even when the unchanged cold-reopen acceptance still reports `NATIVE_UNDO_SEMANTIC_DIVERGENCE`.
- No licensed BricsCAD execution or acceptance conclusion is transferred to the delegated lane. After merge, this root claim should rerun the unchanged exact-SHA matrix and publish the four-field discriminator tuple to issue `#1005`.

## 2026-08-15 post-Undo marker classification continuation

- Exact clean candidate `8a5fbb2ca6b406a5ad4776da1b110b4d863af37b` retained the licensed result `undo_coherent=false` / `redo_coherent=true`; generated native entities participated in Undo, while reconciled semantics remained after-state. The predecessor discriminator did not retain a decisive post-Undo marker class on that failure path.
- `/root` explicitly delegated one automation-only continuation to `/root/local004_postundo_marker` under claim `2026-08-15-codex-local004-post-undo-marker-classification.md`. That successor owns only probe/runner/focused-gate capture of the post-Undo native marker relative to the private before/after tokens and may publish only `BEFORE`, `AFTER`, or `OTHER_OR_INVALID` (plus the same optional post-Redo class).
- This root claim retains licensed execution, private evidence, complete LOCAL-004 acceptance and result publication. Production Source Reconcile coordinator/service/native-marker behavior remains excluded from the delegated lane. If a later licensed run returns post-Undo `AFTER`, the bounded non-local production handoff is explicit ModelSpace block-table-record undo recording; this delegation does not implement it.

### Classification source handoff

- Exact implementation commit `eea8822bcb962dd01fc126d1fd046c5b67e07170` captures the post-Undo marker before the semantic coherence check and persists it in a dedicated exact-key diagnostic. Only `BEFORE`, `AFTER`, or `OTHER_OR_INVALID` may be published; an optional post-Redo class uses the same allowlist.
- All six focused Source Reconcile gates and the PowerShell parser passed. The installed-reference V25 `Release|x64` adapter/Core build completed with `0 warnings / 0 errors`, and both ProductVersion values identify exact commit `eea8822bcb962dd01fc126d1fd046c5b67e07170`.
- The root LOCAL-004 owner should run the licensed matrix on an exact clean descendant containing that commit. A post-Undo `AFTER` receipt bounds the next production lane to explicit ModelSpace block-table-record undo recording; this source handoff does not promote LOCAL-004 or alter production behavior.

## 2026-08-15 database Undo lifecycle diagnostic matrix

Three exact licensed candidates changed only the Source Reconcile revision carrier's
existing-object XData enrollment and all returned the same sanitized result: native
generated output was removed by Undo while the revision marker remained `AFTER`.
The tested variants were ModelSpace BTR late object enable, ModelSpace BTR
ForRead/object-enable/UpgradeOpen, and the existing ModelSpace BlockBegin entity with
the same read/enable/upgrade sequence. This disproves further carrier substitution as
an evidence-backed production fix.

This existing LOCAL-004 claim is expanded before implementation to own one additive,
repository-synthetic V25 diagnostic matrix that isolates the database-level Undo
lifecycle without changing Source Reconcile production behavior:

- new `src/QS3D.BricsCAD.V25/SourceReconcileUndoLifecycleProbeCommands.cs`;
- new `scripts/test-bricscad-v25-source-reconcile-undo-lifecycle.ps1`;
- new `scripts/preflight-source-reconcile-undo-lifecycle-probe.py`;
- this claim and the existing LOCAL-004/Source Reconcile handoff notes for the
  sanitized result only.

Each variant must run in a fresh disposable repository-sample drawing/process and
mutate one existing object's XData plus one topology sentinel in the same native
transaction. The exact variants are `OBJECT_ONLY`, `DB_ENABLE_OBJECT`,
`DB_START_OBJECT`, and `DB_ENABLE_DB_START_OBJECT`. Output is restricted to the
variant plus allowlisted classes for database recording at entry/after enable/after
start and for existing-object/topology state after native Undo. No raw IDs, Handles,
revision tokens, paths, messages, counts, drawing data or private state may be
published. The runner must require a clean exact-SHA build, zero pre-existing
BricsCAD processes, fresh copies, close-without-save, byte-for-byte fixture recovery,
and exact process/script/environment cleanup.

This matrix is diagnostic only. It must not call `Database.StartUndoRecord()` or
database-wide `DisableUndoRecording(false)` from production code, alter the current
Source Reconcile marker/history/event guards, infer `LOCAL_PASS`, or close issue
`#1005`. A production change is allowed only after the matrix distinguishes a
database lifecycle that records the existing-object mutation in the same Undo group
as the topology sentinel. The current operator-owned BricsCAD process is out of
scope; execution waits for the mandatory zero-process boundary.

### Diagnostic implementation handoff

- Claim expansion commit `8dbf11da3ba09f875a926e115b19b8543ef4a608` merged first through PR `#1498` at `de7aba1295abbc113cd548a6f86b8c6462172b2a`.
- Implementation commit `5e319c34b8b9d125c0985784f4263591bbb2f518` merged through PR `#1507` at `3e5bb15b6f55968234bcc2c784c07e770f88439a`. The production Source Reconcile coordinator/service and existing LOCAL-004 runner/probe remain unchanged.
- Exact implementation validation passed: PowerShell AST; seven focused Source Reconcile/manual-policy gates; aggregate preflight `809/809`; Core Release build `0 warnings / 0 errors`; Core smoke `ALL PASS`; installed-reference V25 `Release|x64` build `0 warnings / 0 errors`; adapter/Core ProductVersion suffix `+5e319c34b8b9d125c0985784f4263591bbb2f518`.
- Licensed execution was intentionally not attempted because an operator-owned BricsCAD process was active. The claim remains `ACTIVE / PENDING_LOCAL`; issue `#1005` and LOCAL-004 remain open until a clean exact-SHA, zero-process matrix run returns the four sanitized variant tuples.

### First licensed diagnostic attempt and runner blocker

- The zero-process licensed matrix was attempted on exact current-main SHA `2d101786403bd7526aa47715db325d941a7bcd88` with BricsCAD V25.2.10. PowerShell AST, the three focused Source Reconcile gates and the manual-only CI policy gate passed; the installed-reference V25 `Release|x64` adapter build completed with `0 warnings / 0 errors`.
- The loaded adapter and Core ProductVersion matched the exact SHA. Their SHA-256 values were `7F8707C12F2659BEE56817578A72B41D7C71BA374E258CB3A906895CEBF7F3E0` and `30A4F4A06CFEE6E9C8582D7AAD97207AF71D545DF147615F9AFED0F8D7457CCD`; the repository sample remained at `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967`.
- The runner recovered only the allowlisted `OBJECT_ONLY` tuple: `status=PASS`, `schema=QS3D_SOURCE_UNDO_LIFECYCLE_V1`, `qualification_boundary=LOCAL_004_DIAGNOSTIC_ONLY`, `production_local004_qualified=false`, `db_recording_entry=ON`, both database-recording transition fields `NOT_RUN`, `existing_after_undo=BEFORE`, and `topology_after_undo=UNDONE`. Aggregate metadata was absent and the `DB_ENABLE_OBJECT`, `DB_START_OBJECT`, and `DB_ENABLE_DB_START_OBJECT` variants were not run, so this tuple does not establish a lifecycle conclusion.
- The wrapper terminated with `System.ArgumentException: Argument types do not match`. A source-safe Windows PowerShell 5.1 reproduction shows that converting `System.Collections.Generic.List[object]` with `@($results)` throws the same exception; the runner uses that conversion while constructing aggregate metadata and can therefore mask an earlier qualification error. Non-local source fix issue `#1515` owns the runner repair and focused regression coverage.
- Cleanup passed: zero BricsCAD processes, disposable drawings, private scripts, private sidecars and probe environment values remained, and the fixture hash was unchanged. No private/customer drawing or GitHub Actions run was used. This claim remains `ACTIVE / PENDING_LOCAL`; do not infer `LOCAL_PASS`, close issue `#1005`, or change production Source Reconcile from this partial diagnostic. Re-run the complete four-variant matrix on exact current `main` only after `#1515` merges.

### Post-`#1515` exact rerun and explicit close-before-quit correction

- Exact committed/pushed SHA `d976f547c425291f214dab5e05cb74f0d363c03b` included the merged Windows PowerShell 5.1-safe metadata fix. All seven focused Source Reconcile gates, the runner AST, local handoff/manual-policy guards, full Core smoke `ALL PASS` and the installed-reference V25 `Release|x64` build passed with zero warnings/errors. Adapter/Core ProductVersion matched the exact SHA; their SHA-256 values were `01AD903AA39BA1D33DD5DC738AF0A72CC677379999D44D8EC38E38973A6F84CE` and `A020D559F4D0B3E32FB16FD83B6E1092A6EDCC8871D31BFC87A9545C99F803F2`.
- The licensed V25.2.10 run again produced only the diagnostic `OBJECT_ONLY` tuple (`db_recording_entry=ON`, transition fields `NOT_RUN`, `existing_after_undo=BEFORE`, `topology_after_undo=UNDONE`). The repaired wrapper correctly preserved the original qualification error: BricsCAD had changed the disposable DWG on disk despite the script's bare `QUIT N`, so the remaining variants were not started and no lifecycle winner may be inferred.
- A fresh repository-fixture isolation showed open/`QUIT N` and `NETLOAD`/`QUIT N` preserved the fixture hash. `QS3DSRULPREPARE` followed by bare `QUIT N` instead saved a smaller DWG and created `.bak`; the same command followed by explicit `CLOSE N` and then `QUIT N` preserved the exact fixture SHA-256 `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E` with zero process/backup/lock residue. This is installed-host behavior required to author the minimum local-dependent harness correction.
- The runner now explicitly closes each synthetic DWG without saving before quitting the host, and the focused preflight pins that order. Production Source Reconcile, the probe commands and database Undo behavior remain unchanged. Commit/push the harness correction before rebuild, then rerun all four fresh-process variants on that exact SHA; keep LOCAL-004 and issue `#1005` `ACTIVE / PENDING_LOCAL` until sanitized complete evidence exists.

### Close-fix exact rerun and exact-process finalization correction

- Exact committed/pushed SHA `3856fe92b361180fb944eac9075fe7e4673c69f4` passed all seven Source Reconcile gates, runner AST, local handoff/manual-policy guards, full Core smoke `ALL PASS` and the installed-reference V25 `Release|x64` build with zero warnings/errors. Adapter/Core ProductVersion matched that SHA; their SHA-256 values were `1A3F09B9A82C0F62AE3553B2C767089D2856CC94DD444C2BC80F3619837FB1F8` and `C722634B14F728BC93C94D0D7F2C8E9BF6570F3A765174E05E1C2773B19C8799`.
- The V25.2.10 rerun preserved the synthetic DWG and returned the same diagnostic-only `OBJECT_ONLY` PASS tuple, then refused to start variant two because a BricsCAD process record was still visible immediately after `Wait-Qs3dExit` had observed `HasExited`. Cleanup removed all private/runtime state, and an independent audit moments later found zero BricsCAD processes and the repository fixture unchanged. The remaining variants were not started, so no database-lifecycle conclusion exists.
- A fresh single-process execution of the identical full command sequence used exact-PID `WaitForExit` after host exit. It returned the same `OBJECT_ONLY` tuple, preserved the fixture hash, and observed no BricsCAD process in 30 samples over the following three seconds. The minimum harness correction therefore finalizes the exact test-owned process handle inside `Wait-Qs3dExit` before the next variant's broad zero-process refusal; it does not wait through or absorb an unrelated/operator process.
- The focused gate now pins this exact-process finalization boundary. Production Source Reconcile, probe commands and database Undo behavior remain unchanged. Commit/push, rebuild and rerun all four variants on the new exact SHA before changing LOCAL-004 or issue `#1005` status.

### Complete exact-SHA database Undo lifecycle diagnostic PASS

- Exact committed/pushed SHA `745fb3649463a43d577e5042d8595a4e6f09238f` passed all seven Source Reconcile gates, runner AST, local handoff/manual-policy guards, full Core smoke `ALL PASS` and the installed-reference V25 `Release|x64` build with zero warnings/errors. Adapter/Core ProductVersion ended in that exact SHA; their SHA-256 values were `C9052C3A16AFC7D863021B60735A2BBE6F3C36ED4AFB07EB2B118DE7377DD5DF` and `43FB00EB67C23F92B769615C357C6221FE873F1F62A46BD966BE93A16688F284`.
- Licensed BricsCAD V25.2.10 x64 completed all four fresh-process diagnostic variants. `OBJECT_ONLY` reported database recording `ON / NOT_RUN / NOT_RUN`; `DB_ENABLE_OBJECT` reported `ON / ON / NOT_RUN`; `DB_START_OBJECT` reported `ON / NOT_RUN / ON`; and `DB_ENABLE_DB_START_OBJECT` reported `ON / ON / ON`. Every variant restored the existing BlockBegin marker to `BEFORE` and removed the topology sentinel (`UNDONE`).
- All aggregate cleanup booleans passed: no launched process, private script, sidecar/backup/lock/DWG copy or runner environment value remained. Independent checks agreed, and the repository fixture stayed at SHA-256 `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`. Only four allowlisted marker files plus sanitized metadata were retained outside Git.
- The diagnostic matrix is complete, but `production_local004_qualified=false`. Database Undo recording is already enabled and the default object-only transaction groups the existing mutation with appended topology; database-wide enable and/or `StartUndoRecord` does not distinguish behavior and is not an evidence-backed production correction. Issue `#1005` returns to a non-local owner to isolate the production-specific transaction/command-grouping path. This local worker does not edit coordinator/service production code. The parent claim and LOCAL-004 remain `ACTIVE / PENDING_LOCAL` until the complete production runner passes on a corrected exact SHA.

### Exact production rerun and marker-before-topology continuation

- Root independently reran the complete four-variant diagnostic on exact current-main SHA `e2dbb1e03748047f69a556240f8f85b2e7ccc17e`, BricsCAD V25.2.10 and the exact ProductVersion/plugin hash. All four variants returned the same successful object/topology Undo result recorded above; process/script/private-state/drawing cleanup all passed.
- The unchanged complete production LOCAL-004 runner on that exact SHA still returned `NATIVE_UNDO_SEMANTIC_DIVERGENCE`: generated output was `REMOVED_ALL`, post-Undo and post-Redo marker classes were both `AFTER`, Undo coherence was false, Redo coherence was true, history remained `SYNCED / MULTIPLE`, cold reopen passed, and every cleanup/fixture-restore guard passed. Exact sanitized evidence was posted to issue `#1005`; no IDs, handles, paths, raw revisions or private data were published.
- Source comparison now isolates one production-specific order difference. The successful diagnostic uses the same ModelSpace `BlockBegin`, `DisableUndoRecording(false)`, `UpgradeOpen()` and XData assignment as production, but writes the marker before appending the topology sentinel. Production completes generated invalidation and optional rebuild first, then calls `BeginTransition(...)` and writes the marker from `StageAfter(...)`. Database recording state, carrier type and API sequence are therefore ruled out; marker ordering relative to topology is the remaining bounded variable.

Before production edits, this existing active root claim expands to reserve only:

- `src/QS3D.BricsCAD.V25/Services/SourceReconcileService.cs`;
- `src/QS3D.BricsCAD.V25/SourceReconcileUndoCoordinator.cs`;
- `scripts/preflight-source-reconcile-undo-coherence.py`;
- this claim record and sanitized issue handoff.

The bounded correction will allocate/validate the transition and stage its native marker immediately before `GeneratedDependentGeometryInvalidator.Prepare(...)` inside the same existing transaction. Semantic/native work, after-snapshot capture and private history staging remain afterward; native commit still precedes managed history publication. A failed command still aborts the transaction and removes only a newly registered unpublished history. No `Database.StartUndoRecord`, database-wide recording toggle, new carrier/entity, event-policy relaxation, runner/probe/private-data change or broader Source Reconcile behavior is permitted. LOCAL-004 and `#1005` remain `ACTIVE / PENDING_LOCAL` until the exact corrected production SHA passes the unchanged full runner.

### Exact marker-before-topology result and erase-existing discriminator

- The bounded production change merged through PR `#1606` at exact main SHA `2bb380e343aafdf2d64d23f280858aff7b8ab602`. Exact-merge Source Reconcile gates, Core/Smoke Release build and full smoke, installed-reference V25 `Release|x64` build, and aggregate `824/824` all passed; the adapter ProductVersion matched that SHA.
- The unchanged licensed LOCAL-004 runner still returned `NATIVE_UNDO_SEMANTIC_DIVERGENCE` with post-Undo marker `AFTER`, post-Redo marker `AFTER`, Undo incoherent, Redo coherent and history `SYNCED / MULTIPLE`. Cold reopen and all process/script/private-state/drawing cleanup guards passed. Issue `#1005` comment `#issuecomment-5300651160` records the sanitized receipt.
- `final_generated_state=REMOVED_ALL` is captured by `QS3DSRTAFTERFINALSYNC` before native Undo; it does not classify topology after Undo and must not be used as such. The exact rerun disproves marker ordering alone, without proving a replacement carrier or database lifecycle.

Before further production edits, this active root claim reserves one additive diagnostic-only continuation in the already-owned lifecycle harness:

- `src/QS3D.BricsCAD.V25/SourceReconcileUndoLifecycleProbeCommands.cs`;
- `scripts/test-bricscad-v25-source-reconcile-undo-lifecycle.ps1`;
- `scripts/preflight-source-reconcile-undo-lifecycle-probe.py`;
- this claim and sanitized issue handoff only.

Add exactly one fresh-process `OBJECT_ERASE` variant. Its prepare command must create and commit the existing topology sentinel plus the `BEFORE` marker. Its mutate command must write the same existing BlockBegin marker to `AFTER` first, then erase that existing sentinel in the same transaction. Native Undo must classify only marker `BEFORE|AFTER|OTHER_OR_INVALID` and topology `PRESENT|UNDONE|OTHER_OR_INVALID`; native Redo may use the same allowlists if required. Existing append variants and their results remain unchanged. The runner must keep its repository-sample disposable-copy, zero-process, close-without-save, exact-SHA, privacy and cleanup boundaries.

This discriminator tests the only remaining source-visible operation difference between the successful diagnostic transaction (append) and production invalidation (erase). It must not modify production coordinator/service/invalidation code, introduce an Xrecord/new carrier, infer LOCAL-004 PASS, close `#1005`, publish IDs/Handles/paths/messages/counts, or operate GitHub Actions. A subsequent production change requires the erase result first.

### Erase result and intervening-command discriminator

- Diagnostic implementation `3cf6004af3aaccccadc95547fcf4f4854b80e203` merged through PR `#1612`. Its exact licensed five-process run passed every cleanup boundary. `OBJECT_ERASE` reported database recording `ON`, marker `BEFORE` and topology `PRESENT` after native Undo; the four append controls remained marker `BEFORE` and topology `UNDONE`. Append versus erase is therefore ruled out.
- The complete production runner still differs at the native command boundary: after final `QS3DSYNCSOURCE`, it runs `QS3DSRTAFTERFINALSYNC` and `QS3DSRTMARKERAFTERFINAL` before `UNDO 1`. The successful lifecycle matrix runs `MUTATE` directly followed by `UNDO 1`. The current failure therefore does not yet prove a production marker carrier defect because native Undo may target an intervening modal inspection command.

Before any further production edit, the same diagnostic surfaces above may add exactly one `OBJECT_INSPECTED` fresh-process variant and one automation-only `QS3DSRULINSPECT` command. It must reuse the `OBJECT_ERASE` prepare/mutate transaction, execute the inspection command exactly twice between mutate and `UNDO 1`, and only verify the already-committed `AFTER` marker plus erased sentinel without mutation. The existing `OBJECT_ERASE` direct-Undo control remains in the same matrix. Output schema and allowlists do not change.

This continuation remains diagnostic-only and preserves every exact-SHA/disposable-copy/privacy/cleanup boundary. It must not change production Source Reconcile, use `UNDO Mark/Back` yet, infer LOCAL-004 PASS, close `#1005`, or operate Actions. Only a reproduced marker `AFTER` in `OBJECT_INSPECTED` may authorize a bounded runner correction that targets the reconcile command explicitly.

### Intervening-command result and explicit Undo/Redo boundary correction

- Diagnostic implementation `3fe3178a0faf8dd4984d02554b2f139f0f1552d1` merged through PR `#1616`. The exact licensed six-process matrix passed every cleanup boundary. Direct `OBJECT_ERASE` again returned marker `BEFORE` / topology `PRESENT`; otherwise-identical `OBJECT_INSPECTED`, with exactly two read-only modal inspections before `UNDO 1`, returned marker `AFTER` / topology `UNDONE`. This reproduces the production tuple and proves `UNDO 1` consumed an intervening command instead of the reconcile mutation.
- Issue `#1005` comment `#issuecomment-5300698065` records the sanitized conclusion. No further production marker/coordinator/carrier edit is justified from this failure.

Before implementation, this active root claim expands to reserve only the automation boundary correction:

- `scripts/test-bricscad-v25-source-reconcile.ps1`;
- `src/QS3D.BricsCAD.V25/SourceReconcileRuntimeProbeCommands.cs` only for the exact successful-reconcile count emitted/validated by the existing marker schema;
- `scripts/preflight-source-reconcile-runtime-probe.py`;
- this claim and sanitized issue/result handoff.

Cycle A must select the final sources, set native `UNDO Mark`, run production `QS3DSYNCSOURCE`, capture the existing after-state/marker evidence, then use `UNDO Back` before the existing marker and semantic Undo checks. Cycle B must reselect the same sources, wrap a second production `QS3DSYNCSOURCE` plus existing after-state/marker capture in `UNDO Begin`/`UNDO End`, then execute `UNDO 1` and `REDO` immediately adjacent before the existing Redo marker/semantic checks. The session/cold-reopen marker must report exactly three successful reconciles (initial success plus cycles A/B). The focused gate must lock the two-cycle order, one Mark/Back pair, one Begin/End pair, direct End -> `UNDO 1` -> REDO adjacency, three successful production syncs, and unchanged privacy/cleanup/failure contracts. Single-letter `U` remains forbidden because the Source Reconcile coordinator deliberately refuses that ambiguous V25 command token; both grouped `UNDO 1` and `REDO` must pass through the existing observed-command path.

Production coordinator/service/invalidation/history behavior, lifecycle diagnostic, marker schema/allowlists, source edits, rebuild/save/reopen phases and refusal/rollback/multi-DWG checks remain unchanged. No Actions/private data. LOCAL-004 and `#1005` may become `LOCAL_PASS`/closed only after the complete unchanged-scope runner passes on an exact integrated SHA with zero process and all cleanup guards.

### Explicit-boundary timeout and PICKFIRST ordering correction

- Candidate `c095e4179f2272a384ee699679578f2e1876e6e9` implemented the reserved Mark/Back and Begin/End cycles and passed the runner AST, focused Source Reconcile gate, Core smoke `ALL PASS`, installed-reference V25 `Release|x64` build with exact ProductVersion, and aggregate `824/824` gates.
- Its exact licensed run timed out waiting for the first LOCAL-004 marker. Sanitized metadata retained no phase or result marker, while process/script/private-state cleanup and disposable-drawing restoration all passed and zero BricsCAD processes remained.
- The runner selected the exact sources and then inserted modal `UNDO Mark` or `UNDO Begin` before `QS3DSYNCSOURCE`. Those modal commands consume the PICKFIRST lifetime boundary, so production correctly reaches its interactive fallback with no implied selection and the batch waits. This is the same harness selection-lifetime class already proven in other guarded runtime lanes; it is not a production Source Reconcile failure.

The bounded correction is order-only: each cycle must run `UNDO Mark` or `UNDO Begin` first, then immediately reseed the same exact source pair with `QS3DSRTSELECTSOURCES`, then call `QS3DSYNCSOURCE`. Mark/Back, Begin/End, direct `UNDO 1` -> `REDO`, successful-reconcile count three, marker/semantic checks, and all privacy/cleanup/refusal/rollback/reopen contracts remain unchanged. Only the existing runner and focused gate require this reorder; the probe count change remains valid. Production source and the lifecycle diagnostic remain excluded.
