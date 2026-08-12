# Work claim — Project Interchange timestamp canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-interchange-timestamp-canonicality`
- Registered: `2026-08-12T11:35:00+07:00`
- Baseline main SHA: `773f9e99111a9928c50de5e225613fea7f0694c1`
- Priority: P2 — canonical interchange validation must accept only the exact UTC timestamp representation emitted by the deterministic exporter.

## Confirmed defect

`ProjectInterchangeJsonExporter` requires `DateTimeKind.Utc` and emits project/element timestamps with `value.ToString("O", CultureInfo.InvariantCulture)`. `ProjectInterchangeJsonValidator.ValidateTimestamp(...)`, however, currently accepts any parseable timestamp with an explicit `Z` or numeric offset via `DateTimeOffset.TryParse(...)`.

This means non-canonical equivalents such as `+07:00` or `+00:00`, lowercase/alternate spellings, and other broad-parser forms can pass the canonical validator even though the exporter never emits them. `ProjectInterchangeValidatedSnapshotReader` calls the validator before typed reading, so tightening the validator closes the validated import path without changing reader APIs.

## Reserved scope

- `src/QS3D.Core/Export/ProjectInterchangeJsonValidator.cs` (`ValidateTimestamp(...)` canonical parse only)
- `tests/QS3D.Core.SmokeTests/ProjectInterchangeValidationSmoke.cs` (focused timestamp contract coverage only)
- this claim file

## Intended contract

- Non-empty interchange timestamps must be exact invariant `"O"` tokens produced from a UTC `DateTime`.
- Canonical writer-form timestamps such as `2026-08-10T11:00:00.0000000Z` remain valid.
- Equivalent `+07:00`, `+00:00`, missing-offset, short-form UTC, padded, or otherwise non-canonical timestamp tokens fail validation.
- Preserve the existing missing-timestamp warning policy; this lane changes only validation of non-empty timestamp tokens.
- Preserve all format/unit/category/reference/quantity semantics and validated-reader APIs.

## Excluded scope

- No exporter serialization changes.
- No validated snapshot reader changes unless exact post-claim evidence proves validator-only enforcement is insufficient.
- No Interchange append/import conflict policy changes.
- No GitHub Actions dispatch and no BricsCAD runtime qualification claim.

## Validation plan

- Publish this claim before source writes and verify reachability from current `main`.
- Re-fetch the exact validator/smoke blobs after claim publication.
- Replace broad offset parsing with exact invariant `"O"` UTC parsing plus canonical round-trip equality.
- Extend the existing auto-registered `ProjectInterchangeValidationSmoke` with canonical acceptance and non-canonical offset/short-form rejection.
- Inspect exact diffs/read-back, close this claim with exact SHAs, then ancestry-check claim to current `main`.
- No local compile/runtime PASS will be claimed unless actually executed.
