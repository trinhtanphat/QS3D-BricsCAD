# Agent Work Claim — IFC-02A canonical round-trip projection

- **Agent:** `chatgpt-web-gpt56sol`
- **Date (Asia/Ho_Chi_Minh):** 2026-08-13
- **Status:** `ACTIVE`
- **Workstream:** `IFC-02` — Round-trip preservation mapping
- **Slice:** `IFC-02A` — CAD-independent canonical round-trip projection
- **Priority:** P1
- **Dependency:** IFC-01 acceptance contract (`docs/IFC-ROUND-TRIP-ACCEPTANCE-CRITERIA.md`)

## Why this slice

IFC-01 defines the round-trip acceptance contract, but current `main` has no `IFC-02` claim/implementation and no CAD-independent IFC round-trip projection in `QS3D.Core`. This slice establishes the deterministic Core contract needed before a native V25/V26 IFC adapter can map real IFC entities.

## Reserved scope

Only this claim owns the following new files while `ACTIVE`:

- `src/QS3D.Core/Export/IfcRoundTripProjection.cs`
- `tests/QS3D.Core.SmokeTests/IfcRoundTripProjectionSmoke.cs`
- `tests/QS3D.Core.SmokeTests/IfcRoundTripProjectionRegistration.cs` if the current smoke registration pattern requires a dedicated registration file

Existing smoke registration infrastructure may be minimally updated only if required to register the new smoke case.

## Acceptance

1. Provide a CAD-independent canonical projection for IFC round-trip fields required by IFC-01: stable QS3D identity, IFC global identity, semantic classification, geometry-driving/dimension properties, primary quantity plus unit, and provenance/source lineage.
2. Reject malformed/non-canonical required tokens, non-finite numeric values, duplicate dimension/property names, duplicate IFC identities, and duplicate QS3D identities explicitly instead of first-wins behavior.
3. Canonicalize collection order deterministically without silently changing identity/property payload semantics.
4. Provide tolerance-aware round-trip equivalence for numeric dimension/quantity values while requiring exact identity/classification/unit/provenance semantics.
5. Add focused Core smoke regression covering deterministic ordering, representative supported-family data, tolerance behavior, and duplicate/malformed failure paths.

## Explicit non-scope

- No BricsCAD V25/V26 source changes.
- No native `IfcImport` / `IfcExport` invocation or result-state changes.
- No IFC parser/writer dependency or external IFC SDK.
- No QSDB schema/persistence changes (MAP-01B remains separate).
- No geometry-kernel changes.
- No reopening IFC-01 or issue #982 native-release gates.

## Validation plan

- Inspect current smoke registration conventions before implementation.
- Run available pure Core validation locally if the environment has the required .NET SDK; otherwise perform source-level contract verification and do not claim an unavailable runtime PASS.
- Reconcile latest `main` before substantive commit and again before closing this claim.

## Completion record

Pending implementation and regression commit(s).
