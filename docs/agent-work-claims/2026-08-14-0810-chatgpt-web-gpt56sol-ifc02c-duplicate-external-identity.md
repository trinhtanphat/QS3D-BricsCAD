# Agent Work Claim — IFC-02C duplicate external identity ambiguity

- **Agent:** `chatgpt-web-gpt56sol`
- **Date (Asia/Ho_Chi_Minh):** 2026-08-14
- **Status:** `ACTIVE`
- **Workstream:** `IFC-02` — Round-trip preservation mapping
- **Slice:** `IFC-02C` — duplicate external identity becomes explicit ambiguity
- **Priority:** P1
- **Baseline main SHA:** `5369b54011bba74d15c739a74c66cc7a482347ff`
- **Dependencies:** completed IFC-02A/IFC-02B and `docs/IFC-ROUND-TRIP-ACCEPTANCE-CRITERIA.md`

## Confirmed contract gap

IFC-01 test-matrix row 2 requires duplicate external identity to be reported as ambiguous. IFC-02B introduced an explicit `InvalidOrAmbiguous` result state, but `IfcRoundTripExchangeResultSet.Create()` currently throws on duplicate external identities instead of producing a canonical ambiguous result. This leaves callers responsible for reimplementing the ambiguity transition and can yield inconsistent boundary behavior.

## Reserved scope

- `src/QS3D.Core/Export/IfcRoundTripExchangeResult.cs`
- `tests/QS3D.Core.SmokeTests/IfcRoundTripExchangeResultSmoke.cs`

The existing registration file remains unchanged.

## Acceptance

1. Duplicate canonical external identities are coalesced into exactly one `InvalidOrAmbiguous` result rather than first-wins, double-counting, or generic throw behavior.
2. The coalesced ambiguous result retains only the external identity plus an explicit canonical ambiguity detail; it must not select a trusted QS3D projection, mapping relation, cost relation, or conflicting classification evidence from either duplicate candidate.
3. Unique records retain existing IFC-02B semantics unchanged.
4. Output remains deterministic regardless of input order.
5. Null collection entries and malformed individual records remain fail-closed.
6. Focused regression proves supported-vs-unmapped duplicate, duplicate order reversal, duplicate collapse count, no fabricated trusted identity/relation, and preservation of unique records.

## IFC-02 boundary

This remains schema-neutral CAD-independent Core behavior. No IFC schema/runtime/library selection, native import/export, unit/measurement calculation, mapping/cost business rule, persistence, geometry, Rebar, release automation, LOCAL qualification, or BricsCAD source change is in scope.

**IFC-02 test matrix covered:** row 2 plus deterministic collection behavior relevant to row 15.

## Validation policy

Refresh current `main` before source write and before publication. Use focused managed validation only if an executable .NET compiler is available; otherwise source/read-back verification only, with no runtime PASS claim and no GitHub Actions dispatch.

## Completion record

Pending implementation, regression, merge, remote verification and claim close.
