# QA Gate 2.0 duplicate IFC GUID identity policy

## Contract

`QA2.DUPLICATE_IFC_GUID` is a structural identity conflict in the strict Solibri quantity profile. It is Critical and cannot be released by an element-scoped QA waiver. The conflict remains active for every participant and therefore blocks Takeoff, BOQ, and Estimate while the configured blocking threshold includes Critical findings.

This matches the existing non-waivable treatment for `QA2.DUPLICATE_ELEMENT_ID`: both identifiers participate in provenance, audit, federated-model correlation, and downstream quantity traceability. Allowing every duplicate participant to be waived would make the effective identity ambiguous even when the waiver records themselves are individually auditable.

## Migration and compatibility

The public QA2 API, rule ID, severity profile shape, and waiver DTO remain unchanged. Existing persisted waivers targeting `QA2.DUPLICATE_IFC_GUID` may still be loaded, but they are audit-only and no longer move duplicate-GUID findings into `WaivedFindings` or release the hard gate.

Adapters/importers should resolve duplicate IFC GUIDs at source, regenerate or repair invalid IFC identity where authorized, and rerun QA2. Do not work around the gate by creating matching waivers for every conflicting element.

## Regression coverage

The registered `QsQaGate2Smoke.DuplicateGuidWaiverMustCoverEveryConflictingElement` scenario supplies two otherwise valid elements whose GUIDs differ only by whitespace/case, supplies matching waivers for both participants, and proves that both duplicate findings remain active while Takeoff, BOQ, and Estimate stay blocked.
