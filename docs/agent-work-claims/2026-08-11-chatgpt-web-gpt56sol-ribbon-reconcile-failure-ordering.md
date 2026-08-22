# Work claim — Ribbon reconciliation failure ordering

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-ribbon-failure-ordering`
- Registered: `2026-08-11T20:59:00+07:00`
- Completed: `2026-08-11T21:06:00+07:00`
- Baseline main SHA: `46175c80dcc42c1fd99d252c3d70731c1fddbd75`
- Priority: keep a usable legacy QS3D Ribbon intact until grouped reconciliation has successfully established the replacement panel set.

## Confirmed defect

Before this fix, `RibbonBootstrapper.ReconcileTab(...)` removed the exact legacy `<TAB>_PANEL_SOURCE` before it ensured all current grouped panels/buttons. If reflection/native collection work threw while creating or reconciling a grouped panel, `TryInitialize()` caught the exception and returned `false`, but the old working flat panel had already been removed. A hot reload could therefore degrade from an old-but-usable QS3D Ribbon to a partially reconciled Ribbon solely because migration failed midway.

## Reserved scope

Make legacy flat-panel retirement the final destructive step of a successful per-tab reconciliation. Grouped panel/button reconciliation must happen first; only after all `EnsurePanel(...)` calls complete may the exact old QS3D-owned flat panel be removed. Preserve all completed grouped information architecture, Start Center, dedicated augmenter panels, unknown/user/vendor panels and click-time active-document routing.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs`
- `scripts/preflight-ribbon-information-architecture.py`
- this claim file for close-out

## Excluded scope

- No command regrouping, rename/removal or new Ribbon button.
- No edits to `QuickWorkflowRibbonAugmenter.cs`, Reference Wall, Project Tools, Start Center, updater, Quantity/BQ, Workspace, Core, release/signing or LOCAL inbox.
- No GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim.

## Completion record

- Reservation: `473efae0bc8db1d8bdd4930201c7ad513f9eee1d` — `chore(agent): claim ribbon reconciliation failure ordering`.
- Source fix: `b57acf30f02e2f4c2ad28156cebb9a46a803c106` — `fix(ribbon): retire legacy panels after reconciliation`.
  - `ReconcileTab(...)` now completes every current `EnsurePanel(...)` call before invoking `RemoveLegacyFlatPanel(...)`.
  - The retirement helper and its exact `<TAB>_PANEL_SOURCE` identity remain unchanged, so dedicated/unknown augmenter panels are still preserved.
  - Existing grouped specs, Start Center and click-time `MdiActiveDocument` command routing were not changed.
  - Commit-diff review confirms the product-source change is limited to moving the exact legacy retirement after grouped reconciliation plus explanatory wording; no command catalogue or panel definition changed.
- Regression guard: `ed3f3b81331228f1c60e8d291f482a6089f7ec4d` — `test(ribbon): guard failure-safe legacy retirement`.
  - The existing auto-discovered Ribbon gate now checks the source order `ReconcileTab -> grouped EnsurePanel loop -> legacy retirement -> fresh-tab add`.
  - Existing safeguards remain: no whole collection `.Clear()`, exactly one narrow legacy removal path, current grouped panel/command catalogue and exactly one Start Center binding.

## Integration verification

After concurrent Quantity/Workspace work moved `main` forward, ancestry checks showed both implementation commits remain direct ancestors of current `main`: `b57acf30...` had `behind_by: 0`, and `ed3f3b81...` had `behind_by: 0`, with each commit itself as the merge base. Concurrent changes after these commits did not modify `RibbonBootstrapper.cs`; the only later comparison overlap was the expected preflight commit itself plus unrelated product work.

## Validation boundary

The source and gate were re-fetched/reviewed and their exact commit diffs were inspected. The Python preflight was authored and merged but was **not executed in this connector-only lane**. No GitHub Actions, local checkout/build, BricsCAD launch, installer, signing or release operation was dispatched.

Exact BricsCAD V25 hot-reload failure injection, native Ribbon collection exception behavior, rendering, DPI/Unicode and click dispatch remain `PENDING_LOCAL / DO_NOT_RETRY_REMOTE` under the existing local qualification process. No `LOCAL_PASS` is inferred from source review.

## Coordination

The grouped Ribbon information-architecture, legacy augmenter compatibility, Start Center Ribbon-entry and existing-tab reconciliation claims remain completed. The Create Similar claim still reserves only QuickWorkflow/Create Similar surfaces. The active GitHub auto-update lane remains untouched and continues to forbid Ribbon edits inside its own scope.

## Completion condition

Satisfied for remote/source scope: grouped replacement panels/buttons are established before the exact legacy flat panel can be retired, the source-order regression guard is merged on `main`, ancestry is verified, and native V25 proof remains explicitly local-only.