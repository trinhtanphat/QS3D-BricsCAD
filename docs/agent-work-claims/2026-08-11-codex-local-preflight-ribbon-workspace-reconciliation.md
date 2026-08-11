# Work claim — post-Ribbon/Workspace stale preflight reconciliation

- Status: `COMPLETED`
- Agent: `codex-local-019ff0c5` (`/root/curtain_panel_adapter_audit`)
- Registered: `2026-08-11T20:20:11+07:00`
- Baseline main SHA: `e085c82732d80eb25ba3dcb719715d6ca077b37f`
- Priority: reconcile the aggregate static-gate failures introduced by the completed Ribbon catalogue regrouping and the active Workspace property-origin presentation contract without reverting current product behavior.

## Reserved scope

Audit the aggregate preflight failures reported on the current shared tree and patch only gates proven stale against the current `RibbonBootstrapper` command-catalogue architecture or Workspace explicit property-origin/status UX. Preserve existing command bindings, bootstrap/catalogue structure, semantic edit policy, stale-project guards and Workspace origin labels.

## Expected surfaces

- Only existing failing `scripts/preflight-*.py` files whose assertions are proven stale by current Ribbon or Workspace source.
- Read-only inspection of `src/QS3D.BricsCAD.V25/Ribbon/**` and `src/QS3D.BricsCAD.V25/UI/WorkspacePanel*` / Workspace view-model surfaces.
- This claim file for coordination and close-out.

## Excluded scope

- No product source, Core, tests or product documentation changes unless a real product regression is separately proven and coordinated.
- No `LOCAL-003` Level source, tests, preflights or dirty working-tree files.
- No changes to Ribbon command bindings/catalogue architecture or Workspace origin/edit-policy UX.
- No BricsCAD execution, GitHub Actions, commit, push, release, signing or installer work in this delegated batch.

## Validation plan

- Capture and classify every current aggregate failure as stale gate versus real source regression.
- Update only stale gate assertions to the canonical current source structure while preserving their behavioral contract.
- Run every modified gate, the focused canonical Ribbon/Workspace gates, aggregate preflight if time permits, and `git diff --check`.
- Inspect the final diff to ensure no Level/product source overlap.

## Coordination

The shared worktree contains active LOCAL-003 edits owned by `/root`; they are explicitly out of scope. The completed Ribbon information-architecture implementation is authoritative. The active Workspace property-origin claim owns product UX; this lane only reconciles stale static consumers of that implementation under direct `/root` coordination.

Because `/root` explicitly prohibited commit/push in this delegated batch, this local claim file is a coordination artifact for root integration and is not independently claimed as a pushed reservation.

## Completion condition

Every reported aggregate failure is classified, stale scripts are minimally reconciled and pass focused execution, any real source regression is reported without product edits, and the resulting file list is handed back to `/root` for integration.

Completed on 2026-08-11: all 12 stale Ribbon consumers were moved from the retired `RibbonButtonSpec` literals to the canonical `Button(...)` catalog contract. The stronger Workspace property-origin gate landed independently on `main` and was preserved. Focused gates and the 451-gate aggregate passed; no product source was changed by this claim.
