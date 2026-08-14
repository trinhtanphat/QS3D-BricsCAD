# Work claim — Family Manager QS quick workflow

- Status: `COMPLETED (SOURCE) / PENDING_LOCAL_UI`
- Agent: `chatgpt-web-gpt56sol-family-manager-qs-quick-workflow`
- Registered: `2026-08-14T15:14:00+07:00`
- Baseline main SHA: `c1413daca35dfd611d1ba4d24b015fa4b68bc5c3`
- Owner request: continue remaining owner-reference requirements, fix bugs/update code, commit/push `main`.

## Closed gaps

1. Family Manager now exposes a dedicated category-aware QS form for the common Direct Draw dimensions instead of forcing raw Key/Value entry for routine Family authoring.
2. `Tạo & sử dụng` creates or updates the selected Family, validates/applies QS fields and activates that Family in one rollback-protected project operation.
3. `Lưu & Vẽ` validates draw support before mutation, performs the same atomic Family commit/activation, closes the modal manager and queues the canonical `QS3DDRAWACTIVE` route.
4. The existing New-mode selection ordering race is contained by a late selection handler that restores `_creatingNew` from the actual empty/non-empty Family selection after the original XAML handler runs.
5. `Auto Family` fills the canonical Direct Draw defaults and proposes a collision-free Family name without committing until the owner chooses a commit action.

## Implementation evidence

- Claim: `e07c6d0655b59aa6c89672bb60eec23b13815b0b`
- QS form UI: `cea7a4f66e04059b5d2bb18c2e66dfabe8e52b7d`
- Atomic quick workflow: `a9d7c4438b5bee468261006989ec12cc30199e5e`
- Focused regression guard: `e1c7b48d0e57801796a5555266adccb150b1c75c`

Implemented surfaces:

- `src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.xaml`
- `src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.QuickWorkflow.cs`
- `scripts/preflight-family-manager-qs-quick-workflow.py`

Canonical QS keys remain `WidthM`, `DepthM`, `HeightM`, `ThicknessM`, and `BottomOffsetM`. The workflow reuses `ProjectFamilyService`, `ProjectFamilyActivationService`, `ExecuteAtomic`, and `ActiveFamilyQuickDrawCommands.SupportsFamily`; it does not introduce a second persistence store, native builder, or category dispatcher.

## Direct Draw defaults locked by the focused guard

- Architectural Wall: `ThicknessM=0.2`, `HeightM=3.6`, `BottomOffsetM=0`
- Beam: `WidthM=0.3`, `HeightM=0.5`, `BottomOffsetM=0`
- Column: `WidthM=0.4`, `DepthM=0.4`, `HeightM=3.6`, `BottomOffsetM=0`
- Slab: `ThicknessM=0.12`, `BottomOffsetM=0`
- Glass Wall: `ThicknessM=0.012`, `HeightM=3.6`, `BottomOffsetM=0`
- Wall Pier / Structural Wall: `ThicknessM=0.2`, `HeightM=3.6`, `BottomOffsetM=0`
- Foundation: `ThicknessM=0.5`, `BottomOffsetM=0`

## Validation boundary

Remote source/readback validation is complete. `FamilyManagerWindow.xaml` remains well-formed source, the new partial reads the existing Core Family service signatures, and the focused preflight locks the intended UI/dispatch contract. The repository's most recent observed V25 cloud release run predates these commits, so this claim does **not** invent fresh current-SHA CI evidence.

Exact WPF rendering, modal interaction, click flow, and resulting native Direct Draw inside licensed BricsCAD V25 remain `PENDING_LOCAL_UI` until executed from an artifact built from the exact resulting SHA.

## Excluded scope preserved

- concurrent BLT Ribbon tab contract / `RibbonBootstrapper.cs`
- ProjectState active-context persistability
- Level/Curtain/rebar/runtime lanes owned by other active claims
- quantity/explanation internals
- release workflow mutation
