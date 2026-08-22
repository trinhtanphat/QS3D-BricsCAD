# Plan — Documentation catalog required root sections

## Goal

Make v1 semantic documentation catalog completeness fail closed at the XML schema boundary.

## Implementation

1. Re-fetch `SemanticDocumentationCatalogStore.cs` after claim registration.
2. Replace the root-only `EnsureAtMostOneChild(root, "views"/"sheets")` checks with an exact-one helper.
3. Keep the existing at-most-one helper for optional nested containers.
4. Add focused Core smoke coverage using a catalog produced by `Save(...)`:
   - canonical payload loads;
   - removing `<views>` fails with `InvalidDataException`;
   - removing `<sheets>` fails with `InvalidDataException`;
   - empty-but-present root containers remain valid where planner references permit them.
5. Read back exact diff, verify source/test ancestry on moving `main`, then close the claim.

## Validation boundary

No GitHub Actions, build, release or licensed BricsCAD runtime execution is claimed in this remote lane.
