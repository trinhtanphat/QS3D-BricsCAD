# Work claim — Semantic schedule upsert canonical id

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-semantic-schedule-upsert-canonical-id`
- Registered: `2026-08-12T13:46:00+07:00`
- Baseline main SHA: `6db6f88ff0b5daa516aaf4e6f39d1ccac1d4c521`
- Priority: P1 — canonical-equivalent schedule ids can fail replacement and become duplicate-id save errors.

## Confirmed defect

`SemanticScheduleCatalog.Upsert()` looks up an existing schedule with raw `definition.Id`, while `Remove()`, validation, and serialization use the trimmed/canonical schedule id. An incoming replacement such as `" schedule-1 "` therefore misses existing `"schedule-1"`, is appended, and later fails catalog validation as a duplicate instead of replacing the existing definition.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticScheduleCatalog.cs`, limited to Upsert identity canonicalization.
- `tests/QS3D.Core.SmokeTests/SemanticScheduleCatalogUpsertCanonicalIdSmoke.cs`, focused regression.
- smoke registration only if the current test harness requires it.
- this claim file.

## Intended contract

- Upsert compares with the same required/trimmed id representation used by catalog validation and removal.
- Case-insensitive and whitespace-equivalent ids replace the existing schedule rather than append a second item.
- Persisted schedule remains canonical after Save/Load.
- Existing validation/error boundaries outside this identity lookup remain unchanged.

## Validation boundary

No GitHub Actions, local build/smoke execution, or BricsCAD runtime PASS will be claimed unless actually observed.
