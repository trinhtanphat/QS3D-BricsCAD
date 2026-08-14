# Agent Work Claim — IFC-02E lossless ambiguity integrity

- **Agent:** `chatgpt-web-gpt56sol`
- **Registered (Asia/Ho_Chi_Minh):** 2026-08-14 08:26
- **Status:** `ACTIVE`
- **Workstream:** `IFC-02` — Round-trip preservation mapping
- **Slice:** `IFC-02E` — ambiguous quantity evidence cannot report lossless support
- **Priority:** P1
- **Baseline main SHA:** `1fa65c2167d578f496743000e46a58b87039ee52`
- **Dependencies:** completed IFC-02B/IFC-02C/IFC-02D and `docs/IFC-ROUND-TRIP-ACCEPTANCE-CRITERIA.md`

## Confirmed gap

IFC-02D now makes conflicting per-quantity evidence explicitly visible through `IfcRoundTripProjection.QuantityEvidence.HasAmbiguity`. `IfcRoundTripExchangeResult.IsLosslessSupported`, however, currently returns `true` solely when `State == Supported`. A caller can therefore construct a supported result around a trusted projection whose quantity evidence is ambiguous and receive `IsLosslessSupported == true`, even though IFC-01 requires conflicting duplicate evidence to remain visible and prevents ambiguous/lossy evidence from masquerading as lossless round-trip support.

## Reserved scope

- `src/QS3D.Core/Export/IfcRoundTripExchangeResult.cs`
- `tests/QS3D.Core.SmokeTests/IfcRoundTripExchangeResultSmoke.cs`

No quantity-evidence implementation file, projection implementation, native adapter or registration file is reserved by this slice.

## IFC-02 claim gate for this bounded slice

- **Schema/version + runtime/library boundary:** schema-neutral CAD-independent Core result semantics only; no parser/writer, IFC SDK or BricsCAD runtime.
- **Supported object subset:** existing IFC-02B exchange results that carry an IFC-02A/02D canonical projection.
- **QS3D-to-external identity relation:** unchanged; the existing trusted projection remains authoritative for supported results.
- **Canonical projection:** unchanged `IfcRoundTripProjection`; this slice only consumes its `QuantityEvidence.HasAmbiguity` signal.
- **Result states:** existing enum values remain unchanged. A `Supported` state can still identify a supported object subset, but the convenience predicate `IsLosslessSupported` must not claim losslessness when its retained quantity evidence is ambiguous. Ambiguity remains visible through a dedicated result-level predicate and the projection evidence groups.
- **Reuse boundary:** no quantity/unit conversion, measurement, mapping or cost rule is introduced.
- **Test matrix targeted:** row 10 (conflicting duplicate evidence remains visible), row 12 (no false lossless result when required evidence is not lossless/unambiguous), row 15 (deterministic result semantics).

## Acceptance

1. Add a result-level `HasAmbiguousQuantityEvidence` predicate derived only from the attached canonical projection/evidence set.
2. `IsLosslessSupported` remains true for ordinary `Supported` results with no ambiguous quantity evidence.
3. `IsLosslessSupported` is false for a `Supported` result whose projection has ambiguous quantity evidence.
4. `SupportedLossy`, `Unmapped`, `Unsupported`, and `InvalidOrAmbiguous` continue to report `IsLosslessSupported == false` under existing semantics.
5. Do not mutate the supplied projection or silently discard conflicting evidence.
6. Existing state constructor validation, identity rules, relation evidence and duplicate external-identity coalescing remain unchanged.
7. Focused smoke proves normal supported behavior is preserved and ambiguous quantity evidence cannot masquerade as lossless.

## Explicit non-scope

No change to IFC result enum values, projection/evidence canonicalization, native IFC import/export, unit conversion, measurement formulas, mapping/cost business rules, persistence/QSDB, BricsCAD, Rebar, Cost/CST, Formula, update/release or LOCAL qualification.

## Validation policy

Refresh current `main` and exact claims immediately before source/test writes and publication. Use source/read-back and exact GitHub diff/mergeability verification because this environment has no executable .NET toolchain; do not claim managed/native runtime PASS. No GitHub Actions dispatch.

## Completion record

Pending implementation, focused regression, merge to `main`, remote verification and claim close.
