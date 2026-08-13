# Work claim — IFC-01 round-trip acceptance contract

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-ifc01-roundtrip-acceptance-20260813-1955`
- Registered: `2026-08-13T19:55:00+07:00`
- Baseline main SHA: `2e5935eb62ace97e6524aa5ffe615f2cb09ec338`
- Priority: `IFC-01 / P2`

## Confirmed gap

The current workstream explicitly requires IFC/openBIM round-trip acceptance criteria before broad IFC implementation. Current repository history and source search contain no IFC-01 contract or implemented IFC import/export surface. This lane therefore defines the bounded acceptance contract only; it does not speculate an importer/exporter implementation.

## Reserved scope

- new `docs/IFC-ROUND-TRIP-ACCEPTANCE-CRITERIA.md`
- this claim file

## Intended bounded change

- define identity round-trip semantics for QS3D semantic identity and IFC `GlobalId`;
- define classification and QTO/provenance preservation requirements;
- define supported/unsupported and fail-visible behavior without silently inventing authoritative quantities;
- define deterministic round-trip comparison and duplicate/conflict handling;
- define an executable test matrix that IFC-02 must satisfy once implementation exists;
- preserve product boundary: IFC exchange is an adapter around canonical QS3D semantic/measurement truth, not a replacement for it.

## Excluded scope

- no IFC parser/writer, external IFC library, BCF implementation, native BricsCAD adapter, persistence/schema migration, MAP/CST/QSC/Takeoff source changes, or cross-repo platform migration;
- no GitHub Actions, force-push, or unexecuted managed/native PASS claim.

## Completion condition

Claim-only reservation is published first; overlap is rechecked; the acceptance contract is committed on current `main`; exact remote content/ancestry is verified; claim is closed `COMPLETED` with validation limited to repository/readback checks actually performed.