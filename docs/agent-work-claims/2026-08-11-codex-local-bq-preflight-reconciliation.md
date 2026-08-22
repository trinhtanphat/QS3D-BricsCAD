# Work claim — BQ Summary/Detail preflight reconciliation

- Status: `ACTIVE`
- Agent: `codex-local-019ff0c5` (`/root`, local Windows + BricsCAD V25 agent)
- Registered: `2026-08-11T20:42:50+07:00`
- Baseline main SHA: `af0fc42ea0ee94ea67e5a0bcc4bde42760568e0a`
- Priority: restore aggregate source validation after several already-landed UI/Ribbon refactors without changing product behavior.

## Reserved scope

Reconcile four stale BQ gates with the already-landed `QuantitySummaryWindow` refresh-helper and footer contracts. Preserve the stronger Save-confirmation, current-project, mode-aware refresh, filtering and export ordering.

After the neighboring Project readiness, Quantity Settings, Zone/Family identity and Model Health claims reached `COMPLETED`, also reconcile their stale exact-token gates, restore the shared premium Theme merge omitted by the new Quantity Settings window, and repair their proven nullable-flow adapter build errors. This remains integration-only follow-up to source already present on `main`.

After later concurrent UI/Ribbon/Room/README work landed, the aggregate exposed seven additional failures. Read-only audit proved two are lost README policy/product-boundary statements and five are stale or brittle static assertions whose dedicated feature gates already pass. This claim also owns that bounded documentation/gate reconciliation without modifying the corresponding product source.

## Expected surfaces

- `scripts/preflight-bq-export-freshness.py`
- `scripts/preflight-modeless-review-windows.py`
- `scripts/preflight-schedule-arithmetic.py`
- `scripts/preflight-ui-premium-layout.py`
- `scripts/preflight-project-maintenance-actions.py`
- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml` — shared `Theme.xaml` merge only
- `src/QS3D.BricsCAD.V25/UI/ZoneManagerWindow.xaml.cs`
- `src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.xaml.cs`
- `src/QS3D.BricsCAD.V25/UI/ModelHealthWindow.xaml.cs`
- `scripts/preflight-zone-family-refresh-identity.py`
- `README.md` — restore manual-only workflow and BricsCAD-plugin product-boundary statements only
- `scripts/preflight-ci-manual-only.py` (validation only; source edit only if the documented contract changes)
- `scripts/preflight-product-boundary.py` (validation only; source edit only if the documented contract changes)
- `scripts/preflight-project-tools.py`
- `scripts/preflight-reference-wall-ribbon.py`
- `scripts/preflight-right-panel-quantity-workspace.py`
- `scripts/preflight-room-auto-project-lifecycle.py`
- `scripts/preflight-workspace-quick-draw.py`
- this claim file for close-out

## Excluded scope

- No changes to `QuantitySummaryWindow.xaml`, `QuantitySummaryWindow.xaml.cs`, `Commands.cs`, Core reporting, quantity formulas, CAD locate behavior or user-visible feature behavior.
- No takeover of the active BQ detail/viewport, quantity-description, modeless-project-identity or Core schedule-reporting feature lanes.
- No redesign of Right quantity, Project Tools, Ribbon, Quantity Settings, Model Health or Start Center; only stale post-merge gate reconciliation and the missing shared-theme merge are owned.
- `QuantityInsightPanel.xaml.cs` and its compile/affinity hardening remain owned by the active Quantity Insight affinity claim.
- `RibbonBootstrapper`/augmenter source and `scripts/preflight-ribbon-augmenter-panel-targets.py` remain owned by the active Ribbon augmenter reconciliation claim.
- Updater, Quantity Insight and Wall Quantity nullable compile repairs remain owned by their active claims.
- No BricsCAD/private-DWG qualification, GitHub Actions, release, signing or packaging work.

## Validation plan

- Run the four focused gates and `scripts/preflight-all.py`.
- Run Core Release smoke and the BricsCAD V25 x64 Release build to detect integration regressions from current `main`.
- Run `git diff --check` and verify the final pushed commit against current `origin/main`.

## Coordination

The active BQ/modeless, Quantity Insight, Wall Quantity, Updater and Ribbon augmenter claims continue to own their product surfaces. The Project readiness, Quantity Settings, Zone/Family identity and Model Health claims are `COMPLETED`, releasing their gate/theme/nullable fallout for this bounded integration repair. This lane does not reinterpret their product contract.

## Progress — 2026-08-11

- Implementation `d39c70d5f372458de497fd8e3021cd679d70992a` is reachable from `origin/main` through pushed integration SHA `17ac0fa0`.
- PASS: BQ export freshness, modeless review, schedule arithmetic, premium layout, Project Tools maintenance, Zone/Family identity, Model Health review and shared WPF theme focused gates.
- PASS: Core Release build with 0 warnings/errors and full Core smoke `ALL PASS`.
- Local V25 adapter build proves the owned Zone/Family/Model Health nullable errors are gone. The remaining errors are confined to active Updater, Quantity Insight, Workspace compact-shell and Wall Quantity claims; those files were not touched here.
- Aggregate remains externally red on README manual-only/product-boundary markers plus active Quantity Insight/Ribbon gate drift. No GitHub Actions were dispatched.
- Follow-up reconciliation restored the canonical README product/runtime and `workflow_dispatch`/owner-confirmed release statements. Project Tools and Reference Wall gates now enforce find-or-create/reconcile augmenters; Quantity Insight uses detached preview rows plus current-row locate; Room Auto locates the post-target stale loop; Workspace Quick Draw permits the intentional compact-shell static initializer while still requiring one public instance constructor.
- PASS after the follow-up: manual-only CI, product boundary, Project Tools, Ribbon augmenter targets, Reference Wall Ribbon, Right Quantity workspace, Quantity Insight affinity/preview/single-click, Room Auto lifecycle, Workspace Quick Draw and compact-shell focused gates.

## Completion condition

All owned focused gates and Core smoke pass; the adapter V25 build and aggregate preflight are re-run after separately active Updater/Quantity Insight/Wall Quantity/Ribbon work settles. The only owned product-source edits are the shared-theme merge and behavior-preserving nullable annotations/flow guards; the claim is marked `COMPLETED` once current-main integration is green.
