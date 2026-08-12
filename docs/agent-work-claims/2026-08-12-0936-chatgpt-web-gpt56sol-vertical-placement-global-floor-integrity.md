# Work claim — Level placement global Floor identity integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-vertical-placement-global-floor-integrity-20260812-0936`
- Registered: `2026-08-12T09:36:00+07:00`
- Baseline main SHA: `c22d78d21a115dffc37ccce7e5fd21353675818f`
- Priority: P1 — prevent Level-based quantity/geometry resolution from trusting a globally ambiguous Floor collection.

## Confirmed defect

`ElementVerticalPlacementService.ResolveEffectiveHeight(...)` resolves only the referenced Bottom/Top Floor IDs via `ProjectState.FindFloor(...)`. `FindUnique` detects duplicate IDs only when they match the requested identity. If the project contains unrelated duplicate Floor identities such as `F1` + `f1`, an element using unique `F2/F3` Level references can still obtain a height and downstream regeneration can write quantities/geometry-derived state even though canonical Floor mutation and QSDB persistence reject the project as globally ambiguous.

## Reserved surfaces

- `src/QS3D.Core/Domain/ElementVerticalPlacementService.cs`
- `tests/QS3D.Core.SmokeTests/ElementVerticalPlacementGlobalFloorIntegritySmoke.cs` — new focused regression
- this claim file

## Intended fix

- Only when Level references are present, preflight the complete Floor collection for null entries and case-insensitive duplicate IDs before resolving Bottom/Top Floor identities.
- Preserve legacy-height behavior when no Level references are configured, current bottom/top completeness checks, canonical case-insensitive Floor lookup, offset parsing and finite/positive effective-height rules.
- Focused smoke proves unique F2/F3 references fail closed in the presence of unrelated F1/f1 duplicates, while valid F2/F3 placement still resolves the expected height.

## Coordination

Current ProjectStateSnapshot Zone/Floor identity work owns snapshot semantics only. Completed ProjectFloorService global identity work remains authoritative for mutations; this lane covers Level-placement consumers.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions dispatch; no licensed BricsCAD V25/V26 runtime PASS claimed.
