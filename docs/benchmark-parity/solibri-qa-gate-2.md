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
- unique IFC GUIDs;
- unique QS3D element identities.

Rule severities are configurable per rule. A profile also carries a blocking threshold, allowing project-specific QA policies without changing the analysis engine. `QA2.TYPE_ASSIGNMENT_MISMATCH` is Critical in the strict profile and can be overridden like the other rule severities.

Duplicate IFC GUID analysis is conflict-set based: every element sharing the same non-empty normalized GUID receives its own `QA2.DUPLICATE_IFC_GUID` finding. Normalization trims surrounding whitespace and compares case-insensitively. This preserves element-level auditability and prevents input-order-dependent hard-gate results.

Duplicate element identity analysis is also conflict-set based and case-insensitive. Every participant receives `QA2.DUPLICATE_ELEMENT_ID`. Unlike ordinary model-quality findings, this structural identity conflict is non-waivable because QA2 waiver identity itself is keyed by `(RuleId, ElementId)`; allowing a waiver before element identity is unique would make the audit target ambiguous. Correct or de-duplicate the element identities before Takeoff, BOQ or Estimate can proceed.

## Property/relationship adapter contract

Until IFC adapters expose richer typed relationship objects, snapshots provide normalized evidence through the existing case-insensitive property bag:

- `IfcGuid`
- `IfcPset.<PsetName>`
- `IfcRel.SpatialContainer`
- `IfcRel.TypeAssignment`

Adapters should populate these keys from the authoritative IFC source. `IfcRel.SpatialContainer` must carry the canonical storey/spatial identity represented by `QsModelElementSnapshot.Storey`; `IfcRel.TypeAssignment` must carry the canonical type identity represented by `QsModelElementSnapshot.Type`. Missing, blank, or contradictory evidence fails closed under the strict profile.

## Waivers / exceptions

`QsQaWaiver` is explicit and auditable: rule id, element id, reason, approver, approval UTC timestamp and optional expiry. A waiver applies only to the exact rule + element pair while the evaluation timestamp is within its effective window: at or after `ApprovedUtc`, and at or before `ExpiresUtc` when an expiry exists. A future-dated approval is therefore not active early. Waived findings are retained separately in the gate decision instead of being deleted.

All QA2 time boundaries are explicitly UTC. `approvedUtc`, optional `expiresUtc`, and the `nowUtc` supplied to `QsQaGate2.Evaluate(...)` must have `DateTimeKind.Utc`; Local or Unspecified values fail closed instead of being interpreted through the host machine timezone. This keeps waiver decisions deterministic and audit-equivalent across CI, desktop, service, and regional deployments.

For duplicate IFC GUIDs, waivers remain intentionally element-scoped. Waiving one participant does not release the collision because the other participants retain their own active findings. A host that intentionally accepts a temporary duplicate condition must explicitly waive every conflicting element, preserving a complete audit trail; alternatively, correcting the IFC identities removes the conflict findings normally.

`QA2.DUPLICATE_ELEMENT_ID` is intentionally excluded from waiver application. Even a syntactically matching waiver stays inactive for this rule because two logical elements with the same normalized `ElementId` cannot be distinguished by the waiver key. This is a fail-closed audit-integrity rule rather than a project policy exception.

## Hard gate

When any active finding meets or exceeds the profile blocking threshold, the decision is `Blocked` and all three workflow permissions are false:

- `CanTakeoff`
- `CanBoq`
- `CanEstimate`

`QsQaGate2Decision.DemandAllowed(QsQaGuardedWorkflow)` remains the low-level fail-closed demand check. For QS Intelligence and other workflow-boundary integration, prefer `QsQaGuardedExecutor`: it performs the demand before invoking the supplied Takeoff, BOQ or Estimate work delegate, so blocked workflow code is never executed. Both generic-result and `Action` overloads are available.

This execution boundary is deliberately host-neutral. BricsCAD/UI/API adapters should evaluate QA once for the authoritative snapshot, pass the resulting decision into `QsQaGuardedExecutor`, and place quantity, BOQ or estimating work inside the delegate. UI layers must not invoke the work first and check QA afterward, and must not independently reinterpret severity.

Warnings below the blocking threshold return `PassWithWarnings` and execute normally through the same guarded boundary.

## QS Intelligence integration

Production callers that have the authoritative model/IFC snapshot should use `QsIntelligencePipeline.RunWithQaGate2(...)`. The additive extension accepts the QA snapshot, configurable profile, waivers and a UTC QA evaluation timestamp together with the existing normalized quantity inputs. It evaluates QA once, then wraps the unified Intelligence execution in Takeoff, BOQ and Estimate guarded boundaries before `QsIntelligencePipeline.Run(...)` is invoked.

The authoritative QA snapshot is evaluated before any normalized QS Intelligence downstream work is entered; the quantity records remain downstream business inputs, not substitutes for model-quality evidence.

A blocked decision therefore prevents the Intelligence pipeline from producing revision-derived BOQ or estimate/cost output at all. The returned `QsQaGuardedIntelligenceReport` carries both the authoritative `QsQaGate2Decision` (including waived findings) and the normal `QsIntelligenceReport`, so clients do not need to re-run QA to render audit evidence.

Do not synthesize IFC relationship/Pset evidence from `QsQuantityRecord`: those normalized records do not contain enough typed spatial/material/dimension context to prove Solibri-style completeness. Pass an authoritative `QsModelElementSnapshot` collection from the BIM/IFC adapter instead.

## Compatibility / migration

The earlier `QsQaGate` / `QsQaProfile` API remains available for existing benchmark-foundation callers. Gate 2.0 remains additive. Existing consumers of `CanTakeoff`, `CanBoq`, `CanEstimate`, and `DemandAllowed(...)` continue to work unchanged.

Existing `QsIntelligencePipeline.Run(...)` overloads also remain source/binary compatible as a legacy path. Hosts migrating to the Solibri-parity production gate should switch to `RunWithQaGate2(...)` as soon as they can supply authoritative QA snapshots. The compatibility overloads intentionally do not fabricate IFC evidence or silently change existing behavior.

The duplicate-GUID hardening changes only finding completeness: callers that previously observed one finding for a duplicate pair now observe one finding per conflicting element. Rule id, severity customization, waiver schema, gate API and workflow behavior are unchanged. Consumers should treat findings as element-scoped audit records rather than assuming a single representative finding per GUID collision.

The duplicate-element-identity hardening adds `QA2.DUPLICATE_ELEMENT_ID` to the strict profile and keeps it structurally non-waivable. No public constructor or method signature changes. Integrations that historically supplied duplicate `QsModelElementSnapshot.Id` values must assign stable unique identities before QA2 evaluation; reusing one ID for multiple logical elements is no longer accepted as a waivable condition.

The waiver effective-window hardening does not change the waiver schema or method signatures. It corrects applicability so `ApprovedUtc` is an actual lower bound, matching `ExpiresUtc` as the optional upper bound. Integrations that intentionally schedule future exceptions should continue storing them normally; those exceptions simply remain inactive until their approval timestamp.

The UTC hardening also keeps the public schema and signatures unchanged, but it intentionally stops accepting ambiguous `DateTimeKind.Local` or `DateTimeKind.Unspecified` inputs. Integrations that previously passed those values must normalize at their own application/serialization boundary and construct the QA2 waiver/evaluation timestamps as explicit UTC instants before calling the gate. Do not use `DateTime.SpecifyKind` unless the stored clock value is already known to represent UTC; convert from the source timezone to the correct UTC instant first.

## Smoke coverage

`QsQaGate2Smoke` covers hard blocking, BOQ/estimate/takeoff parity, fail-closed workflow demand, valid, expired and future-dated waivers, severity override behavior, IFC Pset completeness, spatial mismatch, IFC type-assignment mismatch, relationship completeness and duplicate IFC GUID detection. It verifies that future-dated waivers keep findings active until approval and become effective at the approval timestamp. It additionally verifies that Local/Unspecified waiver/evaluation timestamps fail closed, duplicate-GUID findings cover the complete conflict set, case/whitespace normalization is stable, a one-sided duplicate waiver remains blocked, and explicit waivers for every IFC GUID participant remain auditable. Duplicate element IDs are tested case-insensitively as a complete conflict set, remain active despite a matching waiver, and block Takeoff/BOQ/Estimate. It also verifies that `QsQaGuardedExecutor` never invokes blocked work and executes each allowed Takeoff/BOQ/Estimate delegate exactly once.

The registered `QsIntelligenceSmoke` additionally verifies the production integration: a blocking relationship failure prevents downstream Intelligence execution, an explicit valid waiver releases the gate while remaining auditable, and a warning-level profile permits BOQ execution with `PassWithWarnings`.