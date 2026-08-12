# Work claim — Curtain Frame material generated-output scope

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-curtain-frame-material-output-scope`
- Registered: `2026-08-12T11:24:00+07:00`
- Completed: `2026-08-12T11:26:00+07:00`
- Baseline main SHA: `a8afa06a799a620b34e15acfaef4c95b618e7d4a`
- Priority: P2 — prevent unrelated generated-geometry categories from being invalidated by a Glass Wall-only semantic property.

## Confirmed defect

`ElementGeometryPolicy.AffectsGeneratedOutput(...)` treated `CurtainFrameMaterial` as a global generated-output property for every category that requires generated geometry. This was inconsistent with the policy's existing category-scoped curtain geometry keys and caused unrelated categories such as Beam/Slab/Column/ArchitecturalWall to report generated-output impact when an arbitrary `CurtainFrameMaterial` property was edited or removed.

## Delivered contract

- `Material` remains generated-output-affecting for every category that requires generated geometry.
- `CurtainFrameMaterial` is generated-output-affecting only for `ElementCategory.GlassWall`.
- Existing geometry-affecting behavior and category validation stay unchanged.
- No public API signature changes.

## Evidence

- Claim: `cecef9fc369af37cef82c0aece0d123ca021d5e4`
- Plan: `48ff94559a17315ccf1bfe094fd861c89b3e0a7e`
- Source fix: `fec7021557eb82cb712b718885cab9aacdac0bb6`
- Focused smoke: `b6dd1bf0da278a783e79cf85018e76e0bbda2d2c`
- Smoke registration: `95f6b3ee24b248ada23feaa011b34d26f9a00349`
- Static preflight: `c36c2bffd210e95812e04673b209ea9da2f3d540`

Readback on `main` confirmed the scoped source implementation, focused positive/negative smoke cases, ModuleInitializer registration, and static preflight are present after concurrent writes.

## Validation limits

The GitHub connector session did not execute the Core smoke executable, Python preflight, GitHub Actions, or licensed BricsCAD runtime. No PASS is claimed for those execution environments.

## Excluded scope

- Family/material inheritance mechanics.
- Curtain geometry calculation or regeneration implementation.
- UI/editor behavior.
- GitHub Actions/build/release dispatch or licensed BricsCAD runtime qualification.
