# Plan — Documentation catalog named enum tokens

## Goal

Make persisted semantic documentation enum identities unambiguous without changing the catalog format or serializer output.

## Implementation

1. Re-fetch `SemanticDocumentationCatalogStore.cs` from moving `main` after the claim.
2. Replace permissive `Enum.TryParse + IsDefined` load checks for `SemanticViewKind` and `ElementCategory` with named-token validation:
   - required token remains nonblank via existing `Required(...)`;
   - parse case-insensitively;
   - require a defined enum value;
   - require the original token to equal `Enum.GetName(...)` ignoring case, which rejects numeric aliases.
3. Keep all downstream planner validation and symbolic casing compatibility unchanged.
4. Add focused Core smoke coverage that:
   - saves/loads a canonical catalog;
   - accepts lower-case symbolic kind/category tokens;
   - rejects a defined numeric alias for view kind;
   - rejects a defined numeric alias for element category.
5. Read back exact source diff, verify source/test commits remain ancestors of latest `main`, then mark the work claim `COMPLETED`.

## Validation boundary

No GitHub Actions or licensed BricsCAD runtime execution in this remote lane. Regression source and exact GitHub integration evidence are recorded truthfully; no unexecuted PASS claim.
