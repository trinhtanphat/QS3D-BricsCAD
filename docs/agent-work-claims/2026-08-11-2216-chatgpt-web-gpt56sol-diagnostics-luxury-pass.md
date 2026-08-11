# Work claim — Diagnostics luxury consistency pass

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-diagnostics-luxury-pass`
- Registered: `2026-08-11T22:16:00+07:00`
- Baseline main SHA: `cc3203a50f0ab8f90a54ff97e319fdb842eecc80`
- Priority: P1 owner-requested premium / professional / luxury UI continuation

## Goal

Continue the shared QS3D premium/luxury visual language on the two read-only diagnostics/review surfaces that are currently free of active overlapping claims: Audit Log and Model Health. This lane is presentation-only and must preserve every existing diagnostic/filter/locate behavior.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/AuditLogWindow.xaml`
- `src/QS3D.BricsCAD.V25/UI/ModelHealthWindow.xaml`
- `scripts/preflight-diagnostics-luxury-ui.py` (new)
- this claim file for close-out

## Visual contract

- Reuse the existing shared `Theme.xaml`; do not edit the theme in this lane because a concurrent dark-context-menu lane may own it.
- Strengthen surface hierarchy with existing `PremiumCard`, `LuxuryCard`, `StatusPill`, raised-surface and luxury-accent resources.
- Keep the design CAD-first and dense: no blur, glow, shadows, gradients, animation or oversized dashboard chrome.
- Preserve all existing `x:Name`, event handlers, bindings, columns, search/filter/locate semantics and source-DWG lifetime behavior.
- Keep Vietnamese-first labels and truthful read-only/review status language.

## Excluded scope

- `Theme.xaml`, `RightPanel*`, `WorkspacePanel*`, Quantity Insight/Settings/Summary, Start Center, Curtain core/UI, Ribbon, updater/release, Core semantics and all code-behind.
- No command routing, project mutation, diagnostics computation or CAD selection behavior changes.
- No GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim.

## Validation plan

- Re-fetch current `main` and both target XAML blobs before implementation.
- Require well-formed XML and exact preservation of `SearchBox`, `Grid`, `Summary`, `IssueGrid`, `SeverityCombo`, `VisibleCountText`, `SummaryText`, `OnSearchChanged`, `OnFilterChanged`, `OnLocateClick`, and `OnGridDoubleClick`.
- Add an auto-discovered static preflight that requires shared-theme usage plus premium-card/status/luxury hierarchy on both surfaces and rejects heavy effects/negative-margin collision hacks.
- Re-fetch final `main`, confirm ancestry/integration and close this claim with exact SHAs.

## Completion condition

Both diagnostics windows use the existing luxury v2 design system more consistently, behavior is unchanged, the focused static guard is integrated on `main`, the claim is marked `COMPLETED`, and real BricsCAD visual/HiDPI qualification remains local-only.
