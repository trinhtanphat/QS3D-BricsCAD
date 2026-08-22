# Regenerator catalog read-only result plan

## Goal

Make `RegeneratorCatalog.CreateDefault()` honor its `IReadOnlyList<IElementRegenerator>` contract without changing the default catalog contents or regeneration behavior.

## Implementation

1. Re-fetch `main` and `RegenerationEngine.cs` after claim registration.
2. Replace the raw array result with a read-only wrapper over the same ordered five default regenerators.
3. Add a focused Core smoke that verifies count/type order and that index assignment through `IList<IElementRegenerator>` throws `NotSupportedException`.
4. Re-fetch moving `main`, verify source/test commits remain ancestors and no concurrent source overlap invalidated the patch.
5. Close the claim with exact SHAs.

## Exclusions

No dependency ordering changes, no regenerator behavior changes, no BricsCAD adapter changes, no Actions/build/release dispatch and no runtime PASS claim.
