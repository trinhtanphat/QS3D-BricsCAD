# Work claim — issue #3281 native LINE edit qualification

- Status: `ACTIVE`
- Lane-Key: `issue-3281`
- Canonical owner/session: `codex-root-20260820`
- Canonical carrier: `agent/codex/issue3281-native-line-edit-qualification`
- Parent product gap: `#80`
- Baseline main SHA: `969b10096f65f7c6749d97e5ce6aaad21e9eb2ef`

## Confirmed qualification gap

The established LOCAL-004 Source Reconcile matrix is green on exact current
main, including the new #3277 batched ownership path. Its authoritative LINE
and POLYLINE edits are performed through a managed database transaction,
however, so it does not prove the BricsCAD command/editor lifecycle required by
`docs/SOURCE-EDIT-WORKFLOW.md` for native `MOVE`, `ROTATE`, and `STRETCH`.

## Reserved scope

- one additive V25 automation-only native LINE edit runtime probe under
  `src/QS3D.BricsCAD.V25/`
- one guarded exact-SHA PowerShell runner under `scripts/`
- one focused auto-discovered preflight under `scripts/`
- `docs/LOCAL-AGENT-INBOX.md` and `docs/SOURCE-EDIT-WORKFLOW.md` only after an
  exact licensed result is available
- this claim file

## Intended contract

On one repository-sample disposable DWG, author a tracked LINE Wall through
production Direct Draw, build it, and then drive the real native command
processor through these bounded phases:

1. `MOVE` the authoritative source, reconcile, prove displacement with unchanged
   length, remove stale generated ownership, and rebuild safely.
2. `ROTATE` the source, reconcile, prove orientation/placement drift with
   unchanged length, and remove stale generated ownership.
3. `STRETCH` one endpoint through a crossing selection, reconcile, prove source
   and semantic length changed coherently, then rebuild.
4. Save, close and cold-reopen; prove exact source geometry, semantic metrics,
   generated ownership, project affinity and clean runtime state.
5. Fail closed on any unexpected selection/geometry/semantic/generated state and
   verify test-owned process, script, private-sidecar and disposable-drawing
   cleanup without changing the repository fixture.

## Exclusions

- No production `SourceReconcileService`, Undo coordinator, history/marker,
  builders, Direct Draw implementation, geometry, persistence or UI changes.
- No synthetic replacement for native `MOVE`/`ROTATE`/`STRETCH`; the runner
  must issue the actual localized-independent BricsCAD commands.
- No grip/jig/manual ESC qualification, POLYLINE topology/vertex edits,
  Door/Opening, Curtain/rebar/dependent-category matrix, Direct Draw repeated
  mode, issue `#74`, private/customer DWG, release/signing or workflow/Actions
  edits.
- Parent issue `#80` remains open after this P01 even if the exact matrix passes.

## Validation plan

- Claim-first publication before automation code edits.
- Focused gate plus all Source Reconcile guards and PowerShell AST parsing.
- Core Release build/smoke and installed-reference V25 `Release|x64` build from
  one exact clean pushed SHA.
- Licensed BricsCAD V25 execution only on disposable repository-sample copies,
  with sanitized bounded evidence and zero residual processes/private state.
- Protected branch/PR checks and current-main merge only after exact runtime PASS.
