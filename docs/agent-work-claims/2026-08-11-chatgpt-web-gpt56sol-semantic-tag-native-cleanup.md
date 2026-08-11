# Work claim — Semantic Tag native cleanup coverage

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-semantic-tag-native-cleanup`
- Registered: `2026-08-11T21:56:00+07:00`
- Baseline main SHA: `4090f18b4a12062c6143b54f6a8191a472ff5d9e`
- Issues: `#77`, interoperability/native cleanup hardening
- Priority: P1

## Defect

`GeneratedSemanticTagHandles` is a canonical generated ownership slot, but `GeneratedNativeCleanupCoverageGuard` did not advertise an explicit native cleanup handler for it and `GeneratedDependentGeometryInvalidator` did not validate/erase its MText entities or clear the `GeneratedSemanticTag*` metadata family. Any destructive invalidation path protected by the coverage guard therefore failed closed as soon as an affected semantic element owned a generated semantic tag, instead of safely invalidating the tag with the rest of its generated dependents.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Cad/GeneratedNativeCleanupCoverageGuard.cs`
- `src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs`
- `scripts/preflight-interchange-field-merge-native-cleanup-coverage.py`
- this claim file for close-out

## Completion

- Claim: `d947a51af188a9c99da65729c4e863779f0fb8cd`.
- Cleanup coverage guard: `512b9ed1b1d23e99a78237f646892e16b766d4e6`.
- Native invalidator handler: `8e129963e0a0849ff0dd9d3554603d22a0cd3c7a`.
- Static regression gate: `47e2ec4ae2c563a8bd231cedd6bb34f263ca20b9`.
- `GeneratedSemanticTagHealthService.HandlesKey` is now advertised only together with a complete-set liveness/type/ownership validation path and explicit MText erase path.
- Semantic tag erasure requires the metadata owner slot to resolve to the exact `ProjectElement`, requires a live `MText`, and requires matching generated project/element XData ownership before erase.
- `GeneratedSemanticTag*` runtime metadata is swept by `GeneratedGeometryInvalidation.CommitMetadata()` after native preparation/Core apply reaches the existing metadata-commit phase; user-facing `SemanticTagTemplate` and `SemanticTagTextHeightM` do not match that prefix and remain intact.
- Unknown future `Generated*Handle(s)` slots still fail closed in `GeneratedNativeCleanupCoverageGuard`.
- GitHub commit diff/readback: PASS; existing solid/rebar/curtain/grid handler code remained intact.
- Python preflight: NOT RUN in this remote session; gate source was merged/read back only.
- BricsCAD V25 / Windows UI / save-reopen runtime: NOT RUN.
- GitHub Actions: NOT DISPATCHED / NOT RE-RUN.

## Follow-up

MLeader remains a separate #77 feature lane. Do not overload `GeneratedSemanticTagHandles` with MLeader until a distinct MLeader ownership/health/cleanup contract is implemented and exact V25 API calls are proven.