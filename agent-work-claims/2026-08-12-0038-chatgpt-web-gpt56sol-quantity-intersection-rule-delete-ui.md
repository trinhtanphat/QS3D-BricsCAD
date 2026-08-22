# Work claim — Quantity Settings delete directed intersection rule UI

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-intersection-rule-delete-ui-20260812-0038`
- Registered: `2026-08-12T00:38:00+07:00`
- Completed: `2026-08-12T00:44:00+07:00`
- Baseline main SHA: `2341c0903136acb1de9e4502e3c81024f80fe6de`
- Priority: P1 — complete the owner-requested rule authoring workflow so a mistaken/custom directed A → B rule can be removed inside `QS3DSETUP` instead of requiring external JSON editing.

## Confirmed product gap

The existing Intersection Rules browser supported viewing/editing a present A → B rule and explicitly creating a missing A → B rule, but it had no removal action for an existing directed rule. Because missing rules are a supported semantic state and the runtime resolver does not synthesize/mirror absent rules, rule management was one-way inside the product: add/edit was possible, remove required external JSON editing.

## Actual implementation surfaces

- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.CategoryRuleCreation.cs` — the existing window Loaded callback initializes the isolated removal behavior.
- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.IntersectionRuleRemoval.cs` — new isolated partial-class behavior that creates the contextual action programmatically beside the existing create action and owns all removal state/confirmation logic.
- `scripts/preflight-quantity-intersection-rule-delete-ui.py` — focused source contract guard.
- this claim file for close-out.

No XAML edit was ultimately required: the delete button is inserted into the existing `CreateSelectedRuleButton` action panel at runtime. This also avoided conflict with concurrent WPF/base-code-behind work while preserving the existing visual style inherited by `Button`.

## Implementation evidence

- `26bbb8441bd73ceab0ba4d5a007c1871fa6a3d9a` — implementation commit on branch `agent/chatgpt-quantity-rule-delete-20260812-0038`.
- PR #586 — `feat(quantity): remove selected directed intersection rule`.
- `22f2297c9a8f0db3ba93d40efad79bd30f0b9281` — authoritative PR #586 squash merge commit on `main`.

## Final behavior

- Existing selected A → B row exposes `Xóa luật A → B`; missing row continues to expose the existing create action instead.
- Future-schema read-only mode keeps removal disabled.
- Delete click re-resolves the current source/target and exact matching `IntersectionRows` row before confirmation.
- Confirmed deletion removes exactly one in-memory A → B row, then refreshes the existing browser.
- The reverse B → A row and all `CategoryRows` remain untouched.
- No `_store.Save`, Import/Export, direct file/JSON write or CAD/project mutation occurs in the removal handler; persistence remains the existing `Lưu Cài Đặt` action.
- Existing `IntersectionRows.CollectionChanged` wiring refreshes the intersection-only Category Rule diagnostics, and the completed unsaved-close guard treats the removal as an unsaved edit until explicitly saved or discarded.

## Validation actually performed

- Refetched `main` after claim registration and branched from `951a049295e274164a56b9f5b0d13020b1e8230f`.
- Compared that branch base against newer `main` before PR creation; concurrent changes touched `QuantitySettingsWindow.xaml.cs` and other lanes, but did not touch `QuantitySettingsWindow.CategoryRuleCreation.cs`, the new removal partial, or the new focused preflight.
- Reviewed the exact implementation source/patch: read-only check → current source/target → `SingleOrDefault` exact row → explicit Yes/No confirmation → one `IntersectionRows.Remove(selected)` → browser refresh.
- Reviewed the focused preflight source; it pins the exact-row/confirmation/remove ordering and forbids Save/Import/Export/file-write/CategoryRows removal in the delete handler. The preflight was not executed in a local checked-out repository in this remote session.
- GitHub Actions were not dispatched and no licensed BricsCAD V25/WPF runtime PASS is claimed.

## Coordination

The concurrent Quantity Settings import-diagnostics lane owns `QuantitySettingsWindow.xaml.cs`; this implementation deliberately did not edit that file. The global WPF event-contract lane owns only its scanner/docs and explicitly excludes product UI behavior. The active Build3D lane remains untouched.

## Completion condition

Completed: a user can explicitly remove an existing directed A → B intersection rule from `QS3DSETUP` without external JSON editing, with exact-direction confirmation, no auto-save or reverse/category mutation, read-only safety preserved, focused source coverage merged to `main`, and exact merge evidence recorded above.