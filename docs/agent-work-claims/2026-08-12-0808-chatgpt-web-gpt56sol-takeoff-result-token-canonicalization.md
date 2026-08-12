# Work claim — Takeoff result token canonicalization

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T08:08:00+07:00`
- Baseline main SHA: `cc3d339a78546ed9fa06d466f43ce24274b95115`
- Priority: evidence-driven remote-safe Core DTO canonicalization

## Reason

`TakeoffResult` rejects blank handles and units but stores other accepted strings verbatim. Direct public construction therefore permits semantically equivalent tokens such as `"ABCD"` / `" ABCD "` and `"m"` / `" m "` to survive as distinct values. This is inconsistent with `EntitySnapshot`, which canonicalizes its required handle with `Trim()`, and with `QuantityEngine`, which emits canonical fixed unit tokens (`ea`, `m`, `m2`, `m3`).

## Reserved scope

Canonicalize accepted `TakeoffResult` handle and unit tokens with `Trim()` after the existing non-blank validation. Preserve kind/value validation, quantity math, engine-generated values, unit conversion, public property types, and exact behavior for already-canonical tokens. Add focused CAD-independent regression coverage.

## Expected surfaces

- `src/QS3D.Core/Takeoff/TakeoffResult.cs`
- `tests/QS3D.Core.SmokeTests/TakeoffResultIntegritySmoke.cs`
- this claim file

## Excluded scope

- No changes to `QuantityEngine`, wall quantity/reporting, XLSX export, drawing-unit conversion, UI, or BricsCAD V25 runtime.
- No case-folding or semantic unit remapping; trim surrounding whitespace only.
- No GitHub Actions dispatch.

## Validation plan

- Assert accepted surrounding whitespace on handle/unit is canonicalized before storage.
- Preserve existing malformed-state, zero-value, and `QuantityEngine.Calculate()` smoke coverage.
- Re-fetch current `main` and target blobs after the claim lands and before source/test writes; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

The earlier `takeoff-result-integrity` claim is `COMPLETED`; it intentionally preserved valid values and did not reserve canonicalization. Recent takeoff commit history contains no newer active reservation for these two target surfaces at registration time.

## Completion condition

Current `main` stores accepted takeoff handle/unit tokens without surrounding whitespace, includes focused regression coverage, and this claim is marked `COMPLETED`.
