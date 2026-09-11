# Solibri QA Gate 2.0

`QS3D.Core.BenchmarkParity.QsQaGate2` is the benchmark-parity hard gate for quantity workflows. It is intentionally BricsCAD-independent so the same decision can be consumed by BIM adapters, 2D takeoff, BOQ and estimating workflows.

## Strict profile

`QsQaRuleProfile.SolibriQuantityStrict()` requires:

- material and type completeness;
- positive length, width and height;
- IFC property-set evidence for `Pset_Qto` and `Pset_Identity`;
- IFC spatial-containment and type-assignment relationships;
- consistency between the QS3D storey and IFC spatial-container value;
- unique IFC GUIDs.

Rule severities are configurable per rule. A profile also carries a blocking threshold, allowing project-specific QA policies without changing the analysis engine.

## Property/relationship adapter contract

Until IFC adapters expose richer typed relationship objects, snapshots provide normalized evidence through the existing case-insensitive property bag:

- `IfcGuid`
- `IfcPset.<PsetName>`
- `IfcRel.SpatialContainer`
- `IfcRel.TypeAssignment`

Adapters should populate these keys from the authoritative IFC source. Missing or blank evidence fails closed under the strict profile.

## Waivers / exceptions

`QsQaWaiver` is explicit and auditable: rule id, element id, reason, approver, approval UTC timestamp and optional expiry. A waiver applies only to the exact rule + element pair and only while unexpired. Waived findings are retained separately in the gate decision instead of being deleted.

## Hard gate

When any active finding meets or exceeds the profile blocking threshold, the decision is `Blocked` and all three workflow permissions are false:

- `CanTakeoff`
- `CanBoq`
- `CanEstimate`

Warnings below the blocking threshold return `PassWithWarnings`. This contract is designed to be called at workflow boundaries; UI layers should not independently reinterpret severity.

## Compatibility

The earlier `QsQaGate` / `QsQaProfile` API remains available for existing benchmark-foundation callers. Gate 2.0 is additive so current consumers do not require a migration in the same change. New production workflow integration should prefer Gate 2.0.

## Smoke coverage

`QsQaGate2Smoke` covers hard blocking, BOQ/estimate/takeoff parity, valid and expired waivers, severity override behavior, IFC Pset completeness, spatial mismatch, relationship completeness and duplicate IFC GUID detection.
