# Agent work claim — legacy drawing-unit binding canonicality

- Agent: `chatgpt-web-gpt56sol-legacy-drawing-unit-binding-canonicality-20260812-1358`
- Status: `ACTIVE`
- Started: `2026-08-12 13:58 Asia/Ho_Chi_Minh`
- Area: Core drawing-unit legacy quantity binding compatibility
- Scope:
  - `src/QS3D.Core/Units/DrawingUnitResolutionPolicy.cs`
  - `tests/QS3D.Core.SmokeTests/DrawingUnitResolutionSmoke.cs`
- Defect: `TryReadLegacyEffectiveUnit` currently trims persisted `QS3D.DrawingUnit` and truncates at the first space before enum parsing. Malformed values such as `Meter corrupted`, padded values, or numeric aliases can therefore be accepted as a trustworthy legacy quantity-unit binding and bypass the fail-closed compatibility guard.
- Contract:
  - preserve case-insensitive canonical bare named `LengthUnit` tokens produced for historical supported INSUNITS;
  - preserve the historical assumed-millimeter form only as the exact `Millimeter (assumed)` value paired with the exact `QS3D.DrawingUnitAssumption = INSUNITS unsupported/undefined; assumed Millimeter` marker;
  - reject arbitrary suffixes, surrounding whitespace, numeric enum aliases, missing/wrong assumed-unit marker;
  - preserve current bound/override precedence and no-op semantics.
- Validation: focused Core smoke regression source only unless an executable validation is actually run; no GitHub Actions or BricsCAD runtime PASS will be claimed without execution.
