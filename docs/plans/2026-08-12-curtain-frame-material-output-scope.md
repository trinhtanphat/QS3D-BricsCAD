# Plan — Curtain Frame material generated-output scope

## Problem

`ElementGeometryPolicy.AffectsGeneratedOutput(...)` currently places `CurtainFrameMaterial` beside global `Material`, so every category with generated geometry treats that curtain-only key as output-affecting. This creates false stale/generated-output invalidation for unrelated categories.

## Contract

- Keep `Material` global for categories that require generated geometry.
- Scope `CurtainFrameMaterial` to `ElementCategory.GlassWall` only.
- Preserve all existing geometry-affecting keys and their category scope.
- Preserve undefined-category validation and null/blank property behavior.

## Implementation

Refactor only the generated-output key check in `ElementGeometryPolicy` so generic output properties and Glass Wall-specific output properties are represented separately.

## Regression

Add deterministic Core smoke coverage proving:

1. `Material` affects generated output for representative generated categories.
2. `CurtainFrameMaterial` affects generated output for `GlassWall`.
3. `CurtainFrameMaterial` does not affect generated output for representative non-curtain generated categories (Beam, Slab, Column, ArchitecturalWall).
4. Existing Glass Wall curtain geometry keys still affect geometry/output.

Register the smoke with a ModuleInitializer and add a static preflight locking the category-scoped source contract and smoke registration.

## Validation limits

No claim is made that GitHub Actions, the Core smoke executable, the Python preflight, or licensed BricsCAD runtime were executed unless separately reported with evidence.
