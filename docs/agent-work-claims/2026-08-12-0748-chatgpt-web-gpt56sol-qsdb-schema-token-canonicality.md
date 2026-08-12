# Work claim — QSDB schema token canonicality

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12`
- Baseline main SHA: `3531367f947a9ecc46adf4a280b976b8fa1edd9f`
- Priority: persistence schema identity canonicality

## Confirmed defect

`ProjectSchemaMigrator.ReadSchema(...)` uses `int.TryParse(..., NumberStyles.Integer, ...)`. That style accepts alternate textual representations such as leading/trailing whitespace and a leading sign, while normal integer parsing also accepts leading zero aliases such as `03`. QS3D serialization writes the schema version in one canonical representation (`3`). The migration boundary therefore currently accepts multiple textual identities for the same persisted schema version before the strict current-schema validator runs.

## Reserved scope

- `src/QS3D.Core/Persistence/ProjectSchemaMigrator.cs`
- focused Core smoke regression
- `docs/plans/2026-08-12-qsdb-schema-token-canonicality.md`
- this claim file

## Contract

1. Schema tokens are unsigned invariant decimal integers with no whitespace/sign/padding aliases.
2. Canonical legacy tokens `1` and `2` still migrate normally.
3. Canonical current token `3` remains accepted.
4. Noncanonical aliases such as `03`, `+3`, ` 3 ` fail closed.
5. No schema-version bump or migration-content changes.

## Non-overlap

- Do not modify changeVersion/category/audit validation, ProjectFileLock, backup recovery, or native BricsCAD code.
- No GitHub Actions dispatch or release publication.

## Closure

Claim before source, planning before implementation, isolated smoke source, exact diff/readback, ancestry `behind_by: 0`, and no unexecuted PASS claims.
