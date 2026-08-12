# QSDB publication full-schema validation

- Agent ID: `chatgpt-web-gpt56sol-qsdb-publication-schema-validation-20260812-1451`
- Status: COMPLETED
- Baseline: `5dd95e6b3468489c09926ea098ea460886d6938a`
- Claim: `f2bff143d6dd199b10d38044574d6a296018d314`
- Source fix: `99c0abf5ab505749683af5836da0c4d1135e1228`
- Regression smoke: `5c346514111e5508a80ea7b2118337be990fef8e`
- Smoke registration: `d99de0dade9a2d94aec165cda72dcb064c382831`
- Scope:
  - `src/QS3D.Core/Persistence/QsdbProjectStore.cs`
  - `tests/QS3D.Core.SmokeTests/QsdbPublicationSchemaValidationSmoke.cs`
  - `tests/QS3D.Core.SmokeTests/QsdbPublicationSchemaValidationRegistration.cs`
  - this claim file

## Defect

`Load()` reaches `QsdbProjectXmlSchemaValidator.ValidateCurrent()` through current-schema migration, but `SaveCore()` gated atomic publication with `ValidateSerializedFile()`, which checked only the root token, schema number, project id, and project name. A serialized current-schema candidate containing other unsupported schema shape could therefore pass the publication gate even though the normal load path rejected the same bytes.

## Resolution

`ValidateSerializedFile()` still rejects a non-current schema version, then applies `QsdbProjectXmlSchemaValidator.ValidateCurrent(root)` to the exact temp document before any `AtomicFileCommit` publication call. The regression smoke proves a valid saved document passes the publication gate and that adding an otherwise shallow-gate-compatible unsupported root attribute is rejected by both the publication gate and normal load path.

## Verification

GitHub read-back confirms the source fix and smoke files are present on `main`. GitHub reported no status checks and no workflow runs for the smoke-registration commit, so no GitHub Actions or licensed BricsCAD runtime PASS is claimed.