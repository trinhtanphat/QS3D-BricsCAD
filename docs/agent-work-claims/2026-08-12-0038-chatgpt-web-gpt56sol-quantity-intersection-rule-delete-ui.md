# Work claim — Quantity Settings delete directed intersection rule UI

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-intersection-rule-delete-ui-20260812-0038`
- Registered: `2026-08-12T00:38:00+07:00`
- Baseline main SHA: `2341c0903136acb1de9e4502e3c81024f80fe6de`
- Priority: P1 — complete the owner-requested rule authoring workflow so a mistaken/custom directed A → B rule can be removed inside `QS3DSETUP` instead of requiring external JSON editing.

## Confirmed product gap

The existing Intersection Rules browser now supports viewing/editing a present A → B rule and explicitly creating a missing A → B rule, but it has no removal action for an existing directed rule. Because missing rules are a supported semantic state (the runtime resolver does not synthesize/mirror absent rules), rule management is currently one-way inside the product: add/edit is possible, remove requires editing the JSON template outside QS3D.

## Reserved scope

- add one contextual `Xóa luật A → B` action beside the existing create action;
- action is visible/enabled only when the exact selected directed row exists and the window is writable;
- re-check exact source/target and row identity at click time, ask explicit confirmation, then remove exactly that one in-memory directed rule;
- never remove or mirror B → A, never remove Category Rules, never save automatically;
- refresh the existing category/intersection browser after removal, preserving the existing Save-only persistence and unsaved-close guard.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml`
- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.IntersectionRuleRemoval.cs` (new isolated partial)
- `scripts/preflight-quantity-intersection-rule-delete-ui.py` (new focused source gate)
- this claim file for close-out

## Explicit exclusions / coordination

- Current ACTIVE Quantity Settings import-diagnostics claim owns `QuantitySettingsWindow.xaml.cs`; this lane does not edit that file or its preflight.
- Current ACTIVE global WPF XAML event-contract claim owns only its new scanner/docs and explicitly excludes product XAML/partial-class changes; no file overlap.
- Current ACTIVE Build3D claim owns `Build3DCommands.cs`; no geometry/build file is touched.
- No Quantity Settings store/recovery/cardinality/health changes, Core arithmetic/rule resolver/matrix diagnostic changes, category-rule deletion, Ribbon/Start Center changes, CAD/project mutation, updater/release or GitHub Actions.
- Licensed BricsCAD V25 WPF/runtime qualification remains LOCAL_ONLY.

## Validation gates

- existing selected A → B row exposes delete; missing row exposes create, never both actions at once;
- future-schema read-only mode disables deletion;
- delete handler re-resolves exact selected pair and exact matching row before confirmation;
- Yes removes exactly one `IntersectionRows` row, No/cancel path makes no mutation;
- B → A row and CategoryRows remain untouched;
- no `_store.Save`, Import/Export, file/JSON write or CAD/project lifecycle calls in the delete handler;
- removal triggers existing collection/browser refresh and remains unsaved until `Lưu Cài Đặt`;
- focused static preflight pins XAML/handler/source-only contract;
- no GitHub Actions dispatch.

## Completion condition

A user can explicitly remove an existing directed A → B intersection rule from `QS3DSETUP` without external JSON editing, with exact-direction confirmation, no auto-save or reverse/category mutation, read-only safety preserved, focused source coverage merged to `main`, and this claim marked `COMPLETED` with exact merge evidence.