# Work claim — QSDB relation token canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-qsdb-relation-token-canonicality-20260812-1005`
- Registered: `2026-08-12T10:05:00+07:00`
- Baseline main SHA: `c7fdefbe8fff1d2c76c41bda429989f31788814c`
- Priority: P1 — persisted semantic handle/dependency corruption must fail closed instead of being silently normalized on load.
- Task Key: `CORE-QSDB-RELATION-TOKEN-CANONICALITY`

## Confirmed defect

`QsdbProjectStore.ValidateProject(...)` requires every `ProjectElement.SourceHandles` and `DependsOn` value to be non-empty, unpadded and case-insensitively unique. The serializer writes those values verbatim. However `Load(...)` currently skips blank `<h>`/`<d>` values and calls `.Trim()` before adding nonblank values. A malformed persisted QSDB can therefore contain blank or whitespace-padded source handles/dependencies and be silently repaired into a valid in-memory project before `ValidateProject(...)` sees it. This breaks the store's fail-closed canonical persistence contract.

## Reserved scope

- `src/QS3D.Core/Persistence/QsdbProjectStore.cs`
- `tests/QS3D.Core.SmokeTests/QsdbRelationTokenCanonicalitySmoke.cs`
- this claim file

## Intended contract

- Preserve raw persisted `<h>` and `<d>` text through materialization so the existing canonical list validator observes corruption exactly as stored.
- Blank, whitespace-only and padded source-handle/dependency entries must fail `Load(...)` with `InvalidDataException` rather than being skipped or trimmed.
- Preserve canonical writer output, valid load/save round-trip, duplicate detection, ordering semantics and all other QSDB schema/migration/recovery behavior.
- Do not alter runtime handle normalization outside persisted QSDB loading, dependency graph semantics, UI/native BricsCAD or unrelated persistence fields.

## Validation plan

Focused auto-registered Core smoke first saves a canonical project with source handles and a dependency, verifies canonical round-trip, then mutates the emitted XML separately to padded/blank source-handle and dependency tokens and requires `Load(...)` to reject each malformed persisted form. Re-fetch exact source/claim before writes. No force-push, GitHub Actions dispatch, executable full-smoke PASS or licensed BricsCAD runtime qualification claim unless actually executed.
