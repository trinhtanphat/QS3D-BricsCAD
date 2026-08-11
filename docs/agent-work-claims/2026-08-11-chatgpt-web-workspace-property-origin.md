# Work claim — Workspace property origin/status semantics

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-property-origin`
- Registered: `2026-08-11T20:08:00+07:00`
- Completed: `2026-08-11T20:18:00+07:00`
- Baseline main SHA: `6dd428bc8fc157c01d2a4b7ffa89d0d252df95ba`
- Priority: remove a confirmed Property Inspector UX ambiguity where every read-only row was labeled as CAD-derived even when the row was semantic/system/selection metadata

## Reserved scope

Make the Workspace Property Inspector expose an explicit presentation-only row state/origin label instead of inferring every badge from `IsReadOnly`/`CanReset`. Keep Family, inherited Instance, explicit Instance override, CAD/source-derived, system/identity and multi-selection metadata states distinguishable without changing the underlying edit policy or mutation boundaries.

## Source implementation

- `cb8db4abf1ab0379b7cc77abde1437cb697815c3` — `feat(workspace): expose explicit property row state`
  - `PropertyRowViewModel` now exposes `StateKind`, `StateLabel` and `StateSearchText` with presentation-only states for Family, inherited Instance, Override, CAD/measurement, System, Selection and common/mixed Multi rows.
  - `Group`, `Name`, `IsReadOnly` and `CanReset` changes notify the derived state properties, so a successful Instance edit/reset updates the badge without rebuilding the row.
- `e085c82732d80eb25ba3dcb719715d6ca077b37f` — `feat(workspace): search explicit property state`
  - property filtering searches the explicit state label/aliases instead of treating every `IsReadOnly` row as “CAD”.
- `08028c0d957bd0fc72595b70c0d8853e546963b5` — `feat(workspace): render explicit property origin badges`
  - the XAML badge binds `StateLabel` and colors explicit `StateKind` values rather than deriving text from `IsReadOnly`/`CanReset` triggers;
  - the compact legend now distinguishes `Family • Kế thừa • Override • CAD/đo • Hệ thống • Selection`;
  - search tooltip explicitly includes state filtering.
- `3d70c70115adaad97bd939200db306341195bd20` — `test(workspace): guard explicit property origin badges`
  - the auto-discovered Workspace property-palette gate parses XAML, requires the explicit state contract/search bindings and rejects reintroduction of the legacy `IsReadOnly => CAD` presentation rule.

`WorkspaceViewModel.cs` and `WorkspacePanel.MultiSelectionProperties.cs` were re-read and deliberately left unchanged: their existing group/name construction already supplies the non-mutating presentation context used by the row model (`INSTANCE`, `NGUỒN CAD / ĐO ĐẠC`, `HỆ THỐNG / CHỈ ĐỌC`, `SELECTION`, `KHỐI LƯỢNG / ĐO ĐẠC`, plus common/mixed markers). No duplicate property-origin policy or new mutation path was added there.

## Safety boundary preserved

- No change to `SemanticPropertyEditPolicy` semantics or Core bulk mutation services.
- Single-selection editable/read-only decisions remain on `SemanticPropertyEditPolicy.IsEditablePropertyKey`.
- Multi-selection exact CAD/semantic selection revalidation and rollback-protected `SemanticSelectionBulkEditService` writes are unchanged.
- The new state/search contract is presentation-only; it does not call project mutation, CAD APIs or command dispatch.

## Validation / runtime boundary

- Final pushed XAML/source/preflight surfaces were re-fetched from `main` and the legacy hard-coded CAD badge setter was removed from the Workspace state-pill template.
- The focused source gate was updated, but this GitHub connector lane did not execute a local checkout, Core/V25 build, WPF render, NETLOAD or BricsCAD runtime test.
- GitHub Actions were not dispatched.
- No new local queue item was created: exact Workspace visual/HiDPI/modeless qualification remains under the existing canonical Workspace LOCAL_ONLY coverage; this source lane does not claim `LOCAL_PASS`.

## Coordination

The Create Similar lane remains separately BLOCKED only on its canonical `LOCAL-008` bounded inbox handoff and does not reserve Workspace property files. The Start Center claim explicitly excludes `WorkspacePanel*`. No neighboring active claim was overwritten by this lane.

## Completion condition

Satisfied for remote/source scope: explicit property row origin/status semantics, state-aware filtering, XAML binding and static regression coverage are merged to `main`; native WPF/BricsCAD proof remains local-only and unclaimed.
