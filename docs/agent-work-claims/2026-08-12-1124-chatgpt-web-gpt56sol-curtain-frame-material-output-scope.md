# Work claim — Curtain Frame material generated-output scope

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-curtain-frame-material-output-scope`
- Registered: `2026-08-12T11:24:00+07:00`
- Baseline main SHA: `a8afa06a799a620b34e15acfaef4c95b618e7d4a`
- Priority: P2 — prevent unrelated generated-geometry categories from being invalidated by a Glass Wall-only semantic property.

## Confirmed defect

`ElementGeometryPolicy.AffectsGeneratedOutput(...)` treats `CurtainFrameMaterial` as a global generated-output property for every category that requires generated geometry. This is inconsistent with the policy's existing category-scoped curtain geometry keys and causes unrelated categories such as Beam/Slab/Column/ArchitecturalWall to report generated-output impact when an arbitrary `CurtainFrameMaterial` property is edited or removed.

## Reserved scope

- `src/QS3D.Core/Domain/ElementGeometryPolicy.cs`, limited to generated-output property category scoping
- focused Core smoke regression + ModuleInitializer registration under `tests/QS3D.Core.SmokeTests/`
- focused static preflight under `scripts/`
- `docs/plans/2026-08-12-curtain-frame-material-output-scope.md`
- this claim file

## Intended contract

- `Material` remains generated-output-affecting for every category that requires generated geometry.
- `CurtainFrameMaterial` is generated-output-affecting only for `ElementCategory.GlassWall`.
- Existing geometry-affecting behavior and category validation stay unchanged.
- No public API signature changes.

## Excluded scope

- Family/material inheritance mechanics.
- Curtain geometry calculation or regeneration implementation.
- UI/editor behavior.
- GitHub Actions/build/release dispatch or licensed BricsCAD runtime qualification.
