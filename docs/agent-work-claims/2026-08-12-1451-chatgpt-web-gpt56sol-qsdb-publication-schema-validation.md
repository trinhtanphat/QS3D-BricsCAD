# QSDB publication full-schema validation

- Agent ID: `chatgpt-web-gpt56sol-qsdb-publication-schema-validation-20260812-1451`
- Status: ACTIVE
- Baseline: `5dd95e6b3468489c09926ea098ea460886d6938a`
- Scope:
  - `src/QS3D.Core/Persistence/QsdbProjectStore.cs`
  - `tests/QS3D.Core.SmokeTests/QsdbPublicationSchemaValidationSmoke.cs`
  - `tests/QS3D.Core.SmokeTests/QsdbPublicationSchemaValidationRegistration.cs`
  - this claim file

## Defect

`Load()` reaches `QsdbProjectXmlSchemaValidator.ValidateCurrent()` through current-schema migration, but `SaveCore()` gates atomic publication with `ValidateSerializedFile()`, which currently checks only the root token, schema number, project id, and project name. A serialized current-schema candidate containing other unsupported schema shape can therefore pass the publication gate even though the normal load path would reject the same bytes.

## Contract

The exact temporary QSDB bytes produced by save must pass the same full current-schema XML shape validation before `AtomicFileCommit` publishes them. Existing current-schema version checking, size limits, rollback, backup, and atomic publication semantics remain unchanged.