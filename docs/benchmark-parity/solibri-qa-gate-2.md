# Solibri QA Gate 2.0

`QS3D.Core.BenchmarkParity.QsQaGate2` is the benchmark-parity hard gate for quantity workflows. It is intentionally BricsCAD-independent so the same decision can be consumed by BIM adapters, 2D takeoff, BOQ and estimating workflows.

## Strict profile

`QsQaRuleProfile.SolibriQuantityStrict()` requires:

- material and type completeness;
- positive length, width and height;
- IFC property-set evidence for `Pset_Qto` and `Pset_Identity`;
- IFC spatial-containment and type-assignment relationships;
- consistency between the QS3D storey and IFC spatial-container value;
- consistency between the QS3D element type and IFC type-assignment value;
- unique IFC GUIDs.

Rule severities are configurable per rule. A profile also carries a blocking threshold, allowing project-specific QA policies without changing the analysis engine. `QA2.TYPE_ASSIGNMENT_MISMATCH` is Critical in the strict profile and can be overridden like the other rule severities.

## Property/relationship adapter contract

Until IFC adapters expose richer typed relationship objects, snapshots provide normalized evidence through the existing case-insensitive property bag:

- `IfcGuid`
- `IfcPset.<PsetName>`
- `IfcRel.SpatialContainer`
- `IfcRel.TypeAssignment`

Adapters should populate these keys from the authoritative IFC source. `IfcRel.SpatialContainer` must carry the canonical storey/spatial identity represented by `QsModelElementSnapshot.Storey`; `IfcRel.TypeAssignment` must carry the canonical type identity represented by `QsModelElementSnapshot.Type`. Missing, blank, or contradictory evidence fails closed under the strict profile.

## Waivers / exceptions

`QsQaWaiver` is explicit and auditable: rule id, element id, reason, approver, approval UTC timestamp and optional expiry. A waiver applies only to the exact rule + element pair and only while unexpired. Waived findings are retained separately in the gate decision instead of being deleted.

## Hard gate

When any active finding meets or exceeds the profile blocking threshold, the decision is `Blocked` and all three workflow permissions are false:

- `CanTakeoff`
- `CanBoq`
- `CanEstimate`

Workflow boundaries can additionally call `DemandAllowed(QsQaGuardedWorkflow)` to enforce the decision fail-closed instead of relying on a UI/client to remember a boolean check. A blocked decision throws before Takeoff, BOQ, or Estimate execution. Warnings below the blocking threshold return `PassWithWarnings`. UI layers should not independently reinterpret severity.

## Compatibility

The earlier `QsQaGate` / `QsQaProfile` API remains available for existing benchmark-foundation callers. Gate 2.0 remains additive. Existing consumers of `CanTakeoff`, `CanBoq`, and `CanEstimate` continue to work unchanged; `DemandAllowed` is an additional enforcement option for new QS Intelligence and workflow-boundary integrations.

## Smoke coverage

`QsQaGate2Smoke` covers hard blocking, BOQ/estimate/takeoff parity, fail-closed workflow demand, valid and expired waivers, severity override behavior, IFC Pset completeness, spatial mismatch, IFC type-assignment mismatch, relationship completeness and duplicate IFC GUID detection.
