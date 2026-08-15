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

## 2026-08-15 ModelSpace BTR undo-recording candidate result

- Clean exact SHA `f8f5c867c97999f5590dc207cc45925574a0cfa1`, containing production implementation `86afdf93b58169ef8ce755de459a3e4beedbbe29`, passed the six focused Source Reconcile gates, manual-only and generic preflight, Core Release build, full Core smoke and the installed-reference V25 `Release|x64` build. Plugin/Core ProductVersion ended in the exact SHA; adapter SHA-256 was `9E75D2E5345B766403BAAB21C4201B8C65282BB706FC29BE5F8DB6C63E337B55`.
- The licensed BricsCAD V25.2.10 matrix passed both successful reconciles, generated/ambiguous/multi-document refusal, forced rollback, source preservation, generated replacement and cold reopen. Immediately before native Undo, selection/owner classes were `BOTH_SOURCES/BOTH`, generated state was `REMOVED_ALL`, project/revision/native marker were `CHANGED/ADVANCED/ADVANCED`, and history remained `SYNCED` with `MULTIPLE` entries.
- Final result remained `native_undo / NATIVE_UNDO_SEMANTIC_DIVERGENCE`: `undo_coherent=false`, `redo_coherent=true`, and both privacy-safe post-Undo/post-Redo native marker classes were `AFTER`. The exact runtime receipt therefore disproves `DisableUndoRecording(false)` immediately before the existing ModelSpace XData assignment as sufficient on BricsCAD V25.
- Process, private script, private state and drawing restoration all verified true; launcher handoffs were zero, no BricsCAD process remained and the synthetic repository fixture hash stayed `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`. Sanitized issue handoff is `#1005` comment `#issuecomment-5299901488`. No production source, customer/private drawing or GitHub Actions were used.
- The claim stays `ACTIVE / PENDING_LOCAL`; LOCAL-004 stays `IN_PROGRESS`, issue `#1005` stays open, and production diagnosis returns to a non-local claim-first source lane before another exact-SHA licensed rerun.
