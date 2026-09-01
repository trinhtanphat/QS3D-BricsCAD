# Takeoff result Unicode integrity

Issue: #5201
Lane-Key: `issue-5201`
Runtime: deterministic Core only; no licensed BricsCAD evidence is required.

## Defect

`TakeoffResult` is publicly constructible and already enforces blank, whitespace, control-character, unit-case, enum, finite-value, and signed-zero canonicality. Before #5201, its `Handle` and `Unit` loops did not validate UTF-16 surrogate pairing, so an isolated high or low surrogate could survive public admission even though it is not a valid Unicode scalar sequence.

## Contract

Public takeoff result tokens must be valid UTF-16 scalar text before publication. `EnsureValidUnicodeScalarText` rejects unpaired surrogate code units while preserving a valid high+low surrogate pair exactly. Existing handle whitespace/control semantics and canonical lower-case unit semantics remain unchanged.

## Deterministic validation

Run from repository root:

```text
python scripts/preflight-takeoff-result-unicode-integrity.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
```

The smoke covers isolated high/low surrogates on both public token surfaces, valid supplementary-plane Unicode controls, canonical ASCII tokens, zero values, and normal `QuantityEngine` output.
