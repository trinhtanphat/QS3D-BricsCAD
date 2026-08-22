# Work claim — Project Interchange timestamp canonicality

- Status: `DONE`
- Agent: `chatgpt-web-gpt56sol-interchange-timestamp-canonicality`
- Registered: `2026-08-12T11:35:00+07:00`
- Baseline main SHA: `773f9e99111a9928c50de5e225613fea7f0694c1`
- Priority: P2 — canonical interchange validation must accept only the exact UTC timestamp representation emitted by the deterministic exporter.

## Confirmed defect

`ProjectInterchangeJsonExporter` requires `DateTimeKind.Utc` and emits project/element timestamps with `value.ToString("O", CultureInfo.InvariantCulture)`. The validator previously accepted any parseable timestamp with an explicit `Z` or numeric offset via `DateTimeOffset.TryParse(...)`, so non-canonical equivalents could pass canonical validation even though the exporter never emits them.

`ProjectInterchangeValidatedSnapshotReader` validates before typed reading, so validator-side enforcement closes the validated read/import path without changing reader APIs.

## Reserved scope

- `src/QS3D.Core/Export/ProjectInterchangeJsonValidator.cs` (`ValidateTimestamp(...)` canonical parse only)
- `tests/QS3D.Core.SmokeTests/ProjectInterchangeValidationSmoke.cs` (focused timestamp contract coverage only)
- this claim file

## Implemented contract

- Non-empty interchange timestamps now parse only with exact invariant `"O"` and `DateTimeStyles.RoundtripKind`.
- Parsed values must retain `DateTimeKind.Utc` and reproduce the exact stored token with `ToString("O", CultureInfo.InvariantCulture)`.
- Canonical exporter output remains valid.
- Equivalent non-zero offset, `+00:00`, missing-offset, short-form UTC, and padded tokens fail validation.
- Existing `TIMESTAMP_MISSING` warning behavior is unchanged.
- No exporter, typed reader, format/unit/category/reference/quantity behavior changed.

## Commits

- Claim registration: `5e8b43078a165afbaed0708fe4e47b81d7b81dd1`
- Product fix: `28fb849be9248c8532e908d6baebabd0b069f83d`
- Regression: `ea5d5406344fa3578a5c4711f850d258e155fd3c`

## Validation

- Re-fetched exact validator/test blobs after claim publication.
- Exact product diff changes only timestamp parsing and removes the obsolete permissive-offset helper.
- Exact test diff adds only canonical timestamp validation coverage to the already auto-registered smoke.
- Read-back from current `main` confirms the exact parser and regression are present.
- No GitHub Actions dispatched.
- No local C# compile or BricsCAD V25/V26 runtime PASS claimed.
