# Work claim — Quantity Settings create missing directed rule UI

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-rule-create-ui-20260811-2319`
- Registered: `2026-08-11T23:19:00+07:00`
- Baseline main SHA: `c607ee3b73ba6091d39c45ad5f69d8c05829c1bd`
- Priority: P1 — finish the owner-requested “Tạo rule” workflow inside the existing Quantity Settings UI instead of requiring command-line detour for a missing A -> B pair.

## Reserved scope

Add an explicit in-window action in the existing directed Intersection Rule browser that creates exactly the currently selected missing A -> B row in memory with every subtraction flag disabled. Creation must require confirmation, must not create B -> A, must not persist until the user uses the existing Save flow, and must remain disabled for an existing pair or when no pair is selected.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml`
- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml.cs`
- `scripts/preflight-quantity-rule-create-ui.py` (new)
- this claim file for close-out

## Excluded scope

- `src/QS3D.BricsCAD.V25/Services/QuantitySettingsStore.cs` (currently coordinated with local V25 build/recovery work)
- `src/QS3D.BricsCAD.V25/QuantityRuleCreateCommands.cs` and its existing command contract
- Core quantity settings/rule arithmetic/models
- Ribbon/Start Center, CAD/project mutation, geometry/builders, updater/release
- generated-handle index, semantic-tag, material-rename and other active claims
- GitHub Actions and licensed V25 runtime qualification

## Validation plan

- XAML exposes one named create button wired to one handler;
- selected existing rule disables the create action; missing selected pair enables it unless future-schema persistence is blocked;
- handler re-checks current pair and duplicate state, asks Yes/No confirmation, appends exactly one `QuantityIntersectionRuleRow` built from one default `QuantityIntersectionRuleSetting`, then refreshes the selected detail;
- creation does not call `_store.Save`, Import/Export, project lifecycle APIs, CAD transaction APIs or reverse-rule insertion;
- focused static preflight locks the above contract and preserves existing Save-only persistence behavior;
- no GitHub Actions dispatch and no remote claim of native WPF runtime PASS.

## Coordination

Recent active claims reserve generated-handle index integrity, semantic-tag audit-touch and material rename identity; none owns Quantity Settings XAML/code-behind. Quantity Settings health-export and intersection-browser claims are completed. The local V25 Quantity Settings build claim owns `QuantitySettingsStore.cs` and its recovery gate only; this claim deliberately excludes those files.

## Completion condition

From `QS3DSETUP`, selecting a missing directed pair exposes one explicit create action; confirmed creation adds only A -> B with all flags off, remains an unsaved in-window edit until existing Save, duplicate/existing/cancel paths do not mutate rows, a focused source gate is on current `main`, and this claim is marked `COMPLETED` with exact SHAs.