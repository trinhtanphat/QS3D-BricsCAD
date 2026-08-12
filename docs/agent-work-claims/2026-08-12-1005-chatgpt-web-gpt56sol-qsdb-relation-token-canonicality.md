# Work claim — QSDB relation token canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-qsdb-relation-token-canonicality-20260812-1005`
- Registered: `2026-08-12T10:05:00+07:00`
- Last Updated: `2026-08-12T10:10:00+07:00`
- Baseline main SHA: `c7fdefbe8fff1d2c76c41bda429989f31788814c`
- Source fix SHA: `b4c85122c344429d06d4581d2fa79d8203a2e34a`
- Regression SHA: `ed1c6651d37a425688e5a0c3727d42ca65d66e17`
- Priority: P1 — persisted semantic handle/dependency corruption must fail closed instead of being silently normalized on load.
- Task Key: `CORE-QSDB-RELATION-TOKEN-CANONICALITY`

## Confirmed defect

`QsdbProjectStore.ValidateProject(...)` requires every `ProjectElement.SourceHandles` and `DependsOn` value to be non-empty, unpadded and case-insensitively unique. The serializer writes those values verbatim. `Load(...)` previously skipped blank `<h>`/`<d>` values and called `.Trim()` before adding nonblank values, silently repairing malformed persisted QSDB before canonical validation.

## Completed implementation

- `Load(...)` now preserves raw `<h>` and `<d>` text during materialization.
- Existing `ValidateCanonicalStringList(...)` now sees the persisted token exactly as stored and rejects blank, whitespace-only, padded and duplicate relation values.
- Canonical writer output, valid round-trip, dependency ordering semantics and other QSDB schema/migration/recovery behavior remain unchanged.
- No runtime handle normalization outside QSDB loading was modified.

## Regression evidence

`tests/QS3D.Core.SmokeTests/QsdbRelationTokenCanonicalitySmoke.cs` is auto-registered and covers:

- canonical source handle/dependency save-load round-trip;
- padded source handle rejection;
- blank source handle rejection;
- padded dependency rejection;
- blank dependency rejection.

The source commit diff was read back and confirmed the semantic change is limited to preserving raw persisted handle/dependency tokens before existing validation. Source and regression were re-read directly from `main` after commit.

## Validation boundary

No GitHub Actions were dispatched. No executable full smoke/build or licensed BricsCAD V25/V26 runtime PASS is claimed from this connector-only session.

## Completion condition

Completed: malformed persisted source-handle/dependency tokens now fail closed instead of being silently normalized during QSDB load.
