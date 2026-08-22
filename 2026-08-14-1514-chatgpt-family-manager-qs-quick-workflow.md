# Work claim — Family Manager QS quick workflow

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-family-manager-qs-quick-workflow`
- Registered: `2026-08-14T15:14:00+07:00`
- Baseline main SHA: `c1413daca35dfd611d1ba4d24b015fa4b68bc5c3`
- Owner request: continue remaining owner-reference requirements, fix bugs/update code, commit/push `main`.

## Concrete gaps

1. Family Manager still requires raw Key/Value editing for the common QS dimensions used by Direct Draw.
2. There is no single-step `Tạo & sử dụng` workflow that saves/creates a Family and makes it active atomically.
3. There is no `Lưu & Vẽ` workflow that commits the Family, activates it, closes the modal manager and dispatches the canonical `QS3DDRAWACTIVE` route.
4. `OnNewClick` sets `_creatingNew=true` before clearing `FamilyList.SelectedItem`; the synchronous selection-changed handler can reset `_creatingNew=false`, making new-family save behavior depend on prior selection state.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.xaml`
- `src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.xaml.cs` only for the new-mode race fix
- new `src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.QuickWorkflow.cs`
- new `scripts/preflight-family-manager-qs-quick-workflow.py`
- this claim file

## Implementation boundary

- Preserve the existing catalog/property/assignment UI and all existing handlers.
- Add a category-aware QS quick form using canonical Direct Draw keys `WidthM`, `DepthM`, `HeightM`, `ThicknessM`, `BottomOffsetM`.
- Auto-fill selected-family values when present, otherwise use the same defaults as Direct Draw for Wall/Beam/Column/Slab/GlassWall/WallPier/StructuralWall/Foundation.
- `Tạo & sử dụng`: create/rename + apply validated non-empty quick fields + set active in one rollback-protected project mutation.
- `Lưu & Vẽ`: same commit/activate path, then close the modal window and queue `QS3DDRAWACTIVE`; refuse unsupported categories before mutation.
- Do not introduce a second native builder, persistence store, or category dispatcher.

## Excluded scope

- concurrent BLT Ribbon tab contract claim / `RibbonBootstrapper.cs`
- concurrent ProjectState active-context persistability claim
- Level/Curtain/rebar/runtime/local-evidence lanes
- quantity/explanation engine internals
- release workflow changes

## Validation

- Add a focused static preflight locking button handlers, canonical property keys/defaults, new-mode race guard, active-family mutation and `QS3DDRAWACTIVE` dispatch.
- Read back exact pushed sources and current `main` after each write.
- Native visual/click acceptance remains `PENDING_LOCAL` unless exact licensed BricsCAD V25 evidence exists on the resulting SHA.
