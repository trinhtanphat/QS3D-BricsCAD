# Plan — Documentation catalog version token canonicality

## Goal

Make the persisted semantic documentation catalog version token have one unambiguous textual representation.

## Implementation

1. Re-fetch `SemanticDocumentationCatalogStore.cs` after claim registration.
2. Tighten the existing `Integer(...)` parser used by the root `version` attribute:
   - reject null/empty;
   - parse with `NumberStyles.None` and invariant culture;
   - require the raw token to exactly equal `result.ToString(CultureInfo.InvariantCulture)`.
3. Preserve canonical `1`, the current format version and all serializer/load behavior outside version-token identity.
4. Add focused Core smoke coverage using payloads produced by the real `Save(...)` path for canonical `1`, `01`, `+1`, and ` 1 `.
5. Read back exact diff, verify source/test ancestry on moving `main`, then close the claim.

## Validation boundary

No GitHub Actions, build, release or licensed BricsCAD runtime execution is claimed in this remote lane.
