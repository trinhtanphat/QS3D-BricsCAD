# Work claim — Quantity Settings create missing directed rule UI

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-rule-create-ui-20260811-2319`
- Registered: `2026-08-11T23:19:00+07:00`
- Completed: `2026-08-11T23:29:00+07:00`
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

## Implementation evidence

- `941d06a1fd18ccd0465ab4e7a69b1c148e33f765` — code-behind now detects a missing selected A -> B pair, keeps future-schema read-only state fail-closed, re-checks duplicates, asks explicit Yes/No confirmation, appends exactly one all-false directed row in memory, never synthesizes B -> A, and leaves persistence to the existing Save path.
- `c1d4179d08b5c4bac27ace04914b097c3802c6cb` — Quantity Settings XAML exposes the contextual `Tạo luật A → B` action, initially collapsed/disabled and wired to the new handler.
- `4c4eb7f7d1fd2041ef51bd9bcb7197289adb7fa0` + `3dea782b9aa61d8a2bdc18a91640f64cc92d73a3` — focused static source gate added and its confirmation-order assertion corrected after review.
- Final ancestry check confirmed `3dea782b9aa61d8a2bdc18a91640f64cc92d73a3` is an ancestor of newer concurrent `main`; later intervening files did not touch this Quantity Settings lane.

## Validation actually performed

- Reviewed the exact GitHub patches for both UI source commits after push.
- Reviewed the focused preflight source itself and corrected one false ordering assumption: the future-schema warning has its own earlier `MessageBox.Show`, so the gate now anchors confirmation on `var answer = MessageBox.Show(...)` after duplicate checking.
- Verified the final source contract by inspection: create action state follows exact pair lookup; handler has no `_store.Save/Import/Export`, project lifecycle, CAD transaction, handle-selection or direct JSON serializer call; only the existing `Save_Click` persists settings.
- No GitHub Actions were dispatched.
- No licensed BricsCAD V25/WPF click-through PASS is claimed from this remote session.

## Coordination

Recent neighboring claims remained outside this scope. `QuantitySettingsStore.cs`, Core quantity models/arithmetic, command-line `QS3DRULECREATE`, Ribbon/Start Center and other active agent surfaces were not modified.

## Remaining boundary

Exact BricsCAD V25 rendering/click/focus/HiDPI behavior remains under the repository's existing LOCAL_ONLY UI/runtime qualification boundary; this source batch does not manufacture a local runtime PASS.

## Completion condition

Completed: from `QS3DSETUP`, selecting a missing directed pair now exposes one explicit create action; confirmed creation adds only A -> B with all flags off, remains an unsaved in-window edit until existing Save, duplicate/existing/cancel/future-schema paths remain fail-closed, and the focused source gate is on `main`.