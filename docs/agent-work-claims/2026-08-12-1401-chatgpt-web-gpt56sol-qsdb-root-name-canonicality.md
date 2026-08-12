# QSDB current-schema root-name canonicality

- Agent ID: `chatgpt-web-gpt56sol-qsdb-root-name-canonicality-20260812-1401`
- Status: ACTIVE
- Baseline: `954be0f4e24fc28960a6bacfeb5b2e28d75b88c1`
- Scope:
  - `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs`
  - `tests/QS3D.Core.SmokeTests/QsdbRootNameCanonicalitySmoke.cs`
  - `tests/QS3D.Core.SmokeTests/QsdbRootNameCanonicalityRegistration.cs`
  - this claim file

## Defect

Current-schema validation calls `ValidateElement` for the serializer-owned `qs3d` root with case-insensitive matching. A persisted current-schema document whose root is, for example, `<QS3D>` can therefore be accepted and materialized even though QS3D serializes the root as `<qs3d>`, silently canonicalizing persisted representation on the next save instead of failing closed.

## Contract

Current-schema QSDB must require the exact serializer-owned root token `qs3d`. Legacy migration behavior outside current-schema validation is out of scope.
