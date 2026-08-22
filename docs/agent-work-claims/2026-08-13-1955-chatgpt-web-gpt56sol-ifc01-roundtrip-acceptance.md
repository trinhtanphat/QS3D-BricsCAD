# Work claim — IFC-01 round-trip acceptance contract

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-ifc01-roundtrip-acceptance-20260813-1955`
- Registered: `2026-08-13T19:55:00+07:00`
- Baseline main SHA: `2e5935eb62ace97e6524aa5ffe615f2cb09ec338`
- Priority: `IFC-01 / P2`
- Claim-only commit: `f228a08de8da838d7c188fb0b80bb12262e331c6`
- Implementation commit: `038836cdb2441d30b4f818325a12063109c601f1`

## Completed scope

- added `docs/IFC-ROUND-TRIP-ACCEPTANCE-CRITERIA.md`;
- defined deterministic identity/classification/quantity/unit/provenance/mapping round-trip acceptance semantics;
- defined explicit supported/lossy/unmapped/unsupported/invalid states;
- defined the minimum IFC-02 executable test matrix and claim gate;
- preserved QS3D semantic/measurement truth as canonical and kept IFC as an exchange boundary.

## Excluded scope preserved

No IFC parser/writer, external IFC library, BCF implementation, native BricsCAD adapter, persistence/schema migration, MAP/CST/QSC/Takeoff source change, or cross-repo platform migration was added.

## Validation actually performed

- post-claim `main` refresh showed the claim on current lineage with no IFC overlap;
- exact implementation commit diff was inspected and contains only the new acceptance document;
- current remote history was refreshed after implementation and the IFC document remained current;
- no GitHub Actions, managed smoke executable, native build, or BricsCAD runtime was executed, so no managed/native PASS is asserted.