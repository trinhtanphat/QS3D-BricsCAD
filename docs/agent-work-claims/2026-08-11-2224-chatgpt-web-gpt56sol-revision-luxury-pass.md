# Work claim — Revision review luxury pass

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-revision-luxury-pass`
- Registered: `2026-08-11T22:24:00+07:00`
- Baseline main SHA: `4dfdb9a8032496a9113e3ee8a4770b03723e8cba`
- Priority: P1 owner-requested premium / professional / luxury UI continuation

## Goal

Bring the completed read-only Revision comparison window into the same luxury v2 visual language already used by Workspace, Audit Log and Model Health, while preserving the exact comparison/locate behavior.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/RevisionWindow.xaml`
- `scripts/preflight-revision-luxury-ui.py` (new)
- this claim file for close-out

## Visual contract

- Reuse shared `Theme.xaml` only; do not edit Theme resources in this lane.
- Use existing raised graphite surfaces, restrained champagne hierarchy accents, `PremiumCard` and `StatusPill` resources.
- Keep both quantity and semantic tabs dense and data-first; no decorative dashboard widgets.
- Preserve `Header`, `Tabs`, `Grid`, `SemanticGrid`, `Totals`, `OnLocateClick`, `OnGridDoubleClick`, `OnSemanticGridDoubleClick`, every existing DataGrid column/binding, and the read-only contract.
- Do not add any mutation/apply/save action; this remains a read-only revision review + CAD locate surface.

## Excluded scope

- No code-behind, Core revision arithmetic/snapshots, Theme, RightPanel, Workspace, Quantity, Recognition, Start Center, Ribbon, updater/release or other active lane changes.
- No GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim.

## Validation plan

- Re-fetch current `main` and target blob before implementation.
- Add an auto-discovered source preflight that parses XAML, locks all names/handlers/tab labels/columns, requires premium/luxury hierarchy and rejects heavy effects, negative-margin hacks and behavior-layer tokens.
- Merge through a conflict-checked PR because `main` is receiving concurrent agent commits.
- Re-read final `main` blobs and close this claim with exact SHAs.

## Completion condition

Revision comparison is visually consistent with luxury v2, remains fully read-only with unchanged locate semantics, regression guard is on `main`, claim closes with exact evidence, and runtime/HiDPI qualification remains local-only.
