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
