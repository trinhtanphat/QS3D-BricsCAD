# Work claim — LOCAL-004 post-Undo marker classification

- Status: `COMPLETED`
- Agent: `codex-local004-postundo-marker-20260815` (`/root/local004_postundo_marker`, delegated by `/root`)
- Registered: `2026-08-15T07:41:39+07:00`
- Completed: `2026-08-15T07:49:51+07:00`
- Baseline main SHA: `3f35c2fa2b448fe6572aa0ce6b6b4612441ee0ad`
- Implementation commit: `eea8822bcb962dd01fc126d1fd046c5b67e07170`
- Priority: `LOCAL-004 / issue #1005` — the licensed exact-SHA result at `8a5fbb2ca6b406a5ad4776da1b110b4d863af37b` remained `undo_coherent=false` / `redo_coherent=true`, but the failing path did not retain a decisive post-Undo native-marker class.

## Reserved scope

Continue the approved automation-only LOCAL-004 discriminator. Capture the native Source Reconcile marker immediately after guarded native Undo and classify it only against the already-private before-final and after-final marker tokens as `BEFORE`, `AFTER`, or `OTHER_OR_INVALID`. Optionally capture the same bounded class after Redo when it strengthens the handoff.

The failing runtime path must retain this sanitized classification even when the ordinary session-one success marker is unavailable. No opaque revision token or other native value may leave process-private probe state.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/SourceReconcilePostUndoMarkerProbeCommands.cs`
- `scripts/test-bricscad-v25-source-reconcile.ps1`
- `scripts/preflight-source-reconcile-runtime-probe.py`
- `docs/SOURCE-RECONCILE-GENERATED-OUTPUTS.md` only for the bounded exact-SHA handoff after validation
- `docs/LOCAL-AGENT-INBOX.md` only if an exact licensed result changes the recorded LOCAL-004 handoff
- this claim and the active root LOCAL-004 claim coordination note

## Excluded scope

- No production `SourceReconcileUndoCoordinator`, `SourceReconcileService`, native marker storage, database transaction/undo enrollment, history/observer behavior, or other product behavior
- No BricsCAD execution in this delegated lane; the active root LOCAL-004 owner retains licensed execution and private/local evidence
- No customer/private DWG, raw revision/ID/Handle/path/message/count publication, GitHub Actions, release, signing, installer, V26, BLT/BRC or unrelated source/test/documentation work

## Validation plan

- Require the exact public value allowlist `BEFORE|AFTER|OTHER_OR_INVALID`; reject missing or any other value.
- Require capture ordering at the guarded Undo boundary and fail-safe retention on the known `NATIVE_UNDO_SEMANTIC_DIVERGENCE` path.
- Audit the probe and runner for forbidden raw revisions, IDs, Handles, paths, messages and counts.
- Run the focused Source Reconcile runtime-probe/privacy gate and related local source-safe gates.
- Build the V25 adapter against installed references without launching BricsCAD.
- Do not dispatch GitHub Actions or execute licensed runtime/private-data scenarios.

## Coordination

The active root claim `2026-08-13-codex-local004-source-reconcile-runtime.md` owns the broader licensed matrix and explicitly delegates this narrow automation-only continuation to this agent. The completed predecessor claim `2026-08-14-gpt56sol-issue1005-post-undo-marker-discriminator.md` no longer reserves work; this successor replaces only its inconclusive `ADVANCED|UNCHANGED|MISSING_OR_INVALID` result vocabulary and failure-path retention contract. The production issue `#1005` claim remains authoritative for any eventual coordinator/service change and is not edited here.

## Completion condition

The automation-only discriminator and strict privacy gate are pushed from a current-main descendant, focused gates and the installed-reference V25 build pass, and the active root LOCAL-004 owner receives the exact implementation SHA plus the bounded production implication: if licensed post-Undo classification is `AFTER`, the next non-local production claim may be limited to explicit ModelSpace block-table-record undo recording; no production fix is implemented in this lane.

## Implementation and validation

- Exact implementation commit `eea8822bcb962dd01fc126d1fd046c5b67e07170` writes a dedicated nonce-bound `QS3D_SOURCE_RECONCILE_POST_UNDO_MARKER_V1` diagnostic immediately after guarded native Undo, before semantic coherence is checked. The optional Redo class is added atomically after guarded native Redo.
- The diagnostic accepts exactly the base status/schema/boundary/nonce keys plus `post_undo_marker_class` and, when captured, `post_redo_marker_class`. Each classification is limited to `BEFORE`, `AFTER`, or `OTHER_OR_INVALID`; raw revisions, semantic/native IDs, Handles, paths, messages and counts are absent.
- The runner retains this dedicated diagnostic separately from the ordinary session-one and cold-reopen markers, including a sanitized failure path. The existing production commands, Source Reconcile coordinator/service, acceptance matrix and licensed execution ownership are unchanged.
- The focused runtime-probe, Source Reconcile, Undo-coherence, single-bind, audit-owned-revision and grid-annotation gates passed. The PowerShell runner parsed successfully.
- Installed-reference `Release|x64` V25 build succeeded with `0 warnings / 0 errors`. Both adapter and Core ProductVersion end in `+eea8822bcb962dd01fc126d1fd046c5b67e07170`.
- No BricsCAD process, private data or GitHub Actions were used. The active root LOCAL-004 owner retains the licensed exact-SHA rerun and result publication.
