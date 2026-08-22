# Work claim — QSDB relation token canonicality

- Status: `ABORTED`
- State: `REVERTED`
- Agent: `chatgpt-web-gpt56sol-qsdb-relation-token-canonicality-20260812-1005`
- Registered: `2026-08-12T10:05:00+07:00`
- Last Updated: `2026-08-12T10:16:00+07:00`
- Baseline main SHA: `c7fdefbe8fff1d2c76c41bda429989f31788814c`
- Superseded source commit: `b4c85122c344429d06d4581d2fa79d8203a2e34a`
- Superseded regression commit: `ed1c6651d37a425688e5a0c3727d42ca65d66e17`
- Cleanup PR: `#741`
- Cleanup merge SHA: `f787479d16b48f216e97a58eb961087ad6cf7cb0`
- Task Key: `CORE-QSDB-RELATION-TOKEN-CANONICALITY`

## Why this claim was aborted

The initial audit looked only at `QsdbProjectStore.Load(...)` materialization, where persisted `<h>` and `<d>` values are trimmed/skipped before `ValidateProject(...)`. A deeper read found that this is not the first validation boundary: `ProjectSchemaMigrator.MigrateToCurrent(document)` calls `QsdbProjectXmlSchemaValidator.ValidateCurrent(root)` before `QsdbProjectStore` materializes semantic elements.

`QsdbProjectXmlSchemaValidator.ValidateElements(...)` already validates every source-handle and dependency text token with `ValidateCanonicalText(...)` and rejects blank/whitespace-only/padded values, while also rejecting duplicates. Therefore malformed persisted relation tokens already fail closed before the loader's trimming/skipping code can normalize them.

## Cleanup completed

- Reverted `QsdbProjectStore.cs` to its exact pre-lane blob for this scope.
- Removed `tests/QS3D.Core.SmokeTests/QsdbRelationTokenCanonicalitySmoke.cs` because it duplicated an already-enforced XML-schema contract.
- Used a non-force cleanup branch/PR after moving `main` rejected direct fast-forward attempts.
- PR #741 was squash-merged as `f787479d16b48f216e97a58eb961087ad6cf7cb0` while preserving concurrent main work.

## Existing authoritative protection

- `ProjectSchemaMigrator.MigrateToCurrent(...)` calls `QsdbProjectXmlSchemaValidator.ValidateCurrent(root)` before semantic materialization.
- `QsdbProjectXmlSchemaValidator` already rejects malformed `<h>` / `<d>` text and duplicate relation tokens.

## Validation boundary

No GitHub Actions were dispatched. No executable full smoke/build or licensed BricsCAD V25/V26 runtime PASS is claimed from this connector-only session.

## Final outcome

Aborted/reverted: no production change remains from this false-positive lane. The repository keeps the pre-existing schema-validator protection as the canonical implementation.
