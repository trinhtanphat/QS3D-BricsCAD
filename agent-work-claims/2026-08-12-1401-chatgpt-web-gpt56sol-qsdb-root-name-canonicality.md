# QSDB current-schema root-name canonicality

- Agent ID: `chatgpt-web-gpt56sol-qsdb-root-name-canonicality-20260812-1401`
- Status: COMPLETED
- Baseline: `954be0f4e24fc28960a6bacfeb5b2e28d75b88c1`
- Source fix: `ecc277828b99c04050ce0d322eeb9c3c783a0b49`
- Regression smoke: `553cc7e411b39f413c380fa123b6c0f4f6940dc1`
- Registration: `3eae9560a54f098933792df716df805a5d913398`
- Scope:
  - `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs`
  - `tests/QS3D.Core.SmokeTests/QsdbRootNameCanonicalitySmoke.cs`
  - `tests/QS3D.Core.SmokeTests/QsdbRootNameCanonicalityRegistration.cs`
  - this claim file

## Defect

Current-schema validation called `ValidateElement` for the serializer-owned `qs3d` root with case-insensitive matching. A persisted current-schema document whose root was, for example, `<QS3D>` could therefore be accepted and materialized even though QS3D serializes the root as `<qs3d>`, silently canonicalizing persisted representation on the next save instead of failing closed.

## Resolution

Current-schema QSDB now requires the exact serializer-owned root token `qs3d`. The focused smoke keeps canonical save/load as a control and mutates only the root token to uppercase, requiring `QsdbProjectStore.Load()` to fail with `InvalidDataException`. Legacy migration behavior outside current-schema validation is unchanged.

## Validation boundary

Read-back on `main` at `f28794f88c24dd2275da48804e4ed6549d0ab174` confirmed the exact root-token guard, smoke, and module registration remain present after concurrent writes. No GitHub Actions, local compile/smoke execution, or BricsCAD V25/V26 runtime PASS is claimed in this lane.
