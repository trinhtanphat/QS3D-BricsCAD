# Agent Work Claim — IFC-02D canonical quantity evidence

- **Agent:** `chatgpt-web-gpt56sol`
- **Registered (Asia/Ho_Chi_Minh):** 2026-08-14 08:20
- **Status:** `ACTIVE`
- **Workstream:** `IFC-02` — Round-trip preservation mapping
- **Slice:** `IFC-02D` — per-quantity evidence identity, duplicate collapse, and ambiguity visibility
- **Priority:** P1
- **Baseline main SHA:** `bb27dc0cd65065ae67663ddc32e72cfe26ace220`
- **Dependencies:** completed IFC-02A/IFC-02B/IFC-02C and `docs/IFC-ROUND-TRIP-ACCEPTANCE-CRITERIA.md`

## Confirmed gap

IFC-01 requires supported imported quantity evidence to retain quantity/property key, finite value, unit, external source identity and provenance; exact duplicate evidence must not be counted twice and conflicting duplicate evidence must remain visibly ambiguous. Current `IfcRoundTripProjection` retains dimensions, one primary quantity/unit and a global provenance list, but it has no per-quantity external source identity and no canonical evidence grouping that can distinguish exact duplicate evidence from conflicting evidence for the same quantity/source identity.

## Reserved scope

- `src/QS3D.Core/Export/IfcRoundTripProjection.cs`
- new `src/QS3D.Core/Export/IfcRoundTripQuantityEvidence.cs`
- new `tests/QS3D.Core.SmokeTests/IfcRoundTripQuantityEvidenceSmoke.cs`
- new `tests/QS3D.Core.SmokeTests/IfcRoundTripQuantityEvidenceRegistration.cs`

No existing IFC exchange-result source/test is reserved by this slice.

## IFC-02 claim gate for this bounded slice

- **Schema/version + runtime/library boundary:** schema-neutral CAD-independent Core contract only; no IFC parser/writer, external IFC SDK or BricsCAD native invocation.
- **Supported object subset:** existing canonical `IfcRoundTripProjection` objects when a declared exchange subset includes numeric quantity/property evidence.
- **QS3D-to-external identity relation:** existing IFC-02A projection identities remain authoritative; quantity evidence adds only external evidence/source identity and never fabricates QS3D semantic or rule identity.
- **Canonical projection:** `IfcRoundTripProjection` remains the one comparison projection and gains a backwards-compatible optional quantity-evidence collection rather than introducing a parallel projection/calculation engine.
- **Result states:** IFC-02B/02C exchange states remain unchanged. Evidence conflicts are represented explicitly inside the canonical evidence set and therefore can prevent a caller from describing the supported subset as unambiguous/lossless.
- **Reuse boundary:** no unit conversion, quantity recomputation, mapping or cost calculation is added; evidence values/units are preserved as declared boundary evidence.
- **Test matrix targeted:** row 6 (quantity value/unit survives), row 7 (non-finite rejected), row 8 (positive zero canonicalization), row 10 (exact duplicate cannot double-count; conflict visible), row 11 (evidence provenance survives), row 15 (deterministic projection/evidence ordering).

## Acceptance

1. Each supported quantity-evidence candidate retains canonical quantity key, finite value, unit, external source identity and provenance identity.
2. Numeric signed zero is canonicalized to positive zero at construction.
3. Evidence is grouped deterministically by canonical quantity key + external source identity.
4. Exact duplicate candidates collapse to one canonical candidate and therefore cannot double-count.
5. Differing candidates for the same quantity/source identity remain in one deterministic group marked ambiguous; no candidate is silently selected as authoritative.
6. Existing `IfcRoundTripProjection` constructor remains source-compatible and produces an empty evidence collection when none is declared.
7. A new backwards-compatible projection overload can retain canonical quantity evidence and `IfcRoundTripProjectionComparer.AreEquivalent()` compares it deterministically/tolerance-aware without changing existing dimension/primary-quantity semantics.
8. Focused self-registering Core smoke proves finite/zero handling, source/provenance retention, exact duplicate collapse, conflicting ambiguity, deterministic order and projection equivalence behavior.

## Explicit non-scope

No native IFC import/export, schema/library selection, unit conversion formulas, canonical QS3D measurement calculation, mapping/cost business rules, persistence/QSDB changes, BricsCAD V25/V26 code, Rebar, Cost/CST, update/release UI, LOCAL qualification or current estimate-freshness work.

## Validation policy

Refresh current `main` and exact claims before every source/test write and before publication. Run managed validation only if an executable .NET compiler/runtime is available; otherwise use source/read-back + exact GitHub diff/mergeability verification and do not claim runtime PASS. No GitHub Actions dispatch.

## Completion record

Pending implementation, focused regression, merge to `main`, remote verification and claim close.
