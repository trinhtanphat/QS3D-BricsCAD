# Agent Work Claim — IFC-02B explicit exchange result state

- **Agent:** `chatgpt-web-gpt56sol`
- **Date (Asia/Ho_Chi_Minh):** 2026-08-14
- **Status:** `ACTIVE`
- **Workstream:** `IFC-02` — Round-trip preservation mapping
- **Slice:** `IFC-02B` — explicit exchange result-state envelope
- **Priority:** P1
- **Baseline main SHA:** `24496ec6f7554ddd7cbf4ca026e86cd2dd4c47a0`
- **Dependency:** completed IFC-02A canonical projection and `docs/IFC-ROUND-TRIP-ACCEPTANCE-CRITERIA.md`

## Confirmed gap

IFC-01 requires callers to distinguish supported, supported-but-lossy, unmapped, unsupported, and invalid/ambiguous exchange outcomes. The current `IfcRoundTripProjection` requires a trusted `Qs3dElementId`, so an external IFC object that has no trusted QS3D identity cannot be represented as unmapped/unsupported without fabricating a QS3D identity. The current projection also has no dedicated fields for supported classification identity or optional mapping/cost relation evidence.

## Reserved scope

Only this claim owns these new files while `ACTIVE`:

- `src/QS3D.Core/Export/IfcRoundTripExchangeResult.cs`
- `tests/QS3D.Core.SmokeTests/IfcRoundTripExchangeResultSmoke.cs`
- `tests/QS3D.Core.SmokeTests/IfcRoundTripExchangeResultRegistration.cs`

No existing IFC-02A source file is reserved or modified by this slice.

## IFC-02 claim gate for this bounded slice

- **Schema/version + runtime/library boundary:** schema-neutral CAD-independent Core contract only; no IFC parser/writer, no external IFC SDK, no BricsCAD native invocation, and no claim of a specific native IFC schema/runtime qualification.
- **Supported object subset:** externally identified exchange records whose canonical supported payload is an existing `IfcRoundTripProjection`; unmapped/unsupported/invalid records retain external identity without inventing QS3D identity. Beam/column/plate representative projections remain the focused smoke subset inherited from IFC-02A.
- **QS3D-to-external identity relation:** supported records use the explicit QS3D identity already carried by `IfcRoundTripProjection`; non-supported records do not synthesize one.
- **Canonical projection:** completed IFC-02A `IfcRoundTripProjection` remains canonical for supported fields; IFC-02B wraps it with exchange state/evidence rather than creating alternate quantity business logic.
- **Result states:** explicit `Supported`, `SupportedLossy`, `Unmapped`, `Unsupported`, `InvalidOrAmbiguous` state values remain visible to callers.
- **Reuse boundary:** no new unit conversion, measurement, mapping, or cost formula is introduced. Optional existing classification/mapping/cost relation identities are carried as evidence only.
- **Test matrix rows targeted:** row 3 (unknown external object stays unmapped), row 5 (unsupported explicit), row 12 (loss prevents false lossless state), row 14 (supported existing relation survives while absent relation stays absent), plus deterministic state/evidence ordering where applicable.

## Acceptance

1. Preserve canonical external object identity for every result without requiring a fabricated QS3D identity.
2. Enforce defined result-state enum values and fail closed on malformed canonical tokens.
3. Supported and supported-lossy results require an existing canonical IFC-02A projection whose external identity matches the envelope.
4. Supported-lossy results require an explicit loss reason; ordinary supported results cannot masquerade as lossy.
5. Unmapped/unsupported/invalid-or-ambiguous results do not carry a canonical QS3D projection.
6. Optional classification, mapping and cost relation identities remain distinct and absence remains `null`/absent rather than invented defaults.
7. Focused self-registering Core smoke covers state invariants, external/QS3D identity separation, relation evidence, malformed input and deterministic behavior.

## Explicit non-scope

No BricsCAD V25/V26 source, native IFC import/export, IFC parser/writer dependency, QSDB persistence/schema, geometry kernel, measurement formulas, unit conversion formulas, mapping logic, cost calculation, release automation, Rebar, CST, LOCAL qualification, or current Family-assignment scopes.

## Validation policy

Re-fetch current `main` and overlap before source write and again before publication. Run pure Core checks only if an executable .NET environment is available; otherwise perform source/read-back verification and do not claim managed/native runtime PASS. No GitHub Actions dispatch for this lane.

## Completion record

Pending implementation, focused regression, merge to `main`, remote verification and claim close.
