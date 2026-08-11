# Work claim — Semantic Tag native cleanup coverage

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-semantic-tag-native-cleanup`
- Registered: `2026-08-11T21:56:00+07:00`
- Baseline main SHA: `4090f18b4a12062c6143b54f6a8191a472ff5d9e`
- Issues: `#77`, interoperability/native cleanup hardening
- Priority: P1

## Defect

`GeneratedSemanticTagHandles` is a canonical generated ownership slot, but `GeneratedNativeCleanupCoverageGuard` does not advertise an explicit native cleanup handler for it and `GeneratedDependentGeometryInvalidator` does not validate/erase its MText entities or clear the `GeneratedSemanticTag*` metadata family. Any destructive invalidation path protected by the coverage guard therefore fails closed as soon as an affected semantic element owns a generated semantic tag, instead of safely invalidating the tag with the rest of its generated dependents.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Cad/GeneratedNativeCleanupCoverageGuard.cs`
- `src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs`
- `scripts/preflight-interchange-field-merge-native-cleanup-coverage.py`
- this claim file for close-out

## Contract

- explicitly whitelist `GeneratedSemanticTagHealthService.HandlesKey` only after the invalidator has a matching liveness/ownership/type/erase path;
- validate the complete semantic-tag handle set before any destructive invalidation and require live `MText` plus matching generated project/element ownership;
- erase only ownership-proven generated semantic-tag MText inside the existing native transaction;
- clear generated semantic-tag runtime metadata by the `GeneratedSemanticTag` prefix only after native preparation succeeds; preserve user-facing `SemanticTagTemplate` / `SemanticTagTextHeightM` configuration;
- keep fail-closed behavior for unknown future `Generated*Handle(s)` slots;
- do not add MLeader yet; fix the existing MText lifecycle first;
- no Revision, quantity, updater, recognition, project-name or other active lane changes;
- no GitHub Actions dispatch/re-run and no licensed V25 runtime claim.

## Completion condition

Source readback shows coverage guard + invalidator + focused static gate agree on the Semantic Tag handler, unsupported generated slots remain blocked, and the claim closes with exact pushed SHA(s).