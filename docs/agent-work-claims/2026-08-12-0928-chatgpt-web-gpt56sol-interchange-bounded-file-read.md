# Work claim — Project interchange bounded file read

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:28:00+07:00`
- Baseline main SHA: `d030e1655075b4055cdf46fc451f27b9c58b5a7a`
- Priority: evidence-driven remote-safe interchange input hardening

## Reason

`ProjectInterchangeJsonValidator.ValidateFile()` checks `FileInfo.Length` against the existing 16 MiB `MaxFileBytes` contract, then performs an unbounded `File.ReadAllBytes(fullPath)`. A file that grows after the metadata check can therefore be read/allocated beyond the guarded size before the later string validation rejects it.

## Reserved scope

Make the file-read boundary enforce the existing `MaxFileBytes` contract while bytes are read. Preserve path validation, not-found behavior, strict UTF-8 handling, validation result semantics, the exact 16 MiB public limit, JSON limits, and exporter/importer behavior.

## Expected surfaces

- `src/QS3D.Core/Export/ProjectInterchangeJsonValidator.cs`
- focused regression coverage under `tests/QS3D.Core.SmokeTests/` or the repository's existing interchange validation test surface
- this claim file

## Excluded scope

- No interchange schema/version or collection-limit changes.
- No exporter/import planner changes.
- No BricsCAD command/UI changes.
- No GitHub Actions dispatch.

## Validation plan

- Preserve rejection of an already-oversize file.
- Lock a bounded streaming/read helper so no `File.ReadAllBytes` path can bypass `MaxFileBytes` after the initial metadata check.
- Preserve valid UTF-8 validation and invalid UTF-8 error behavior.
- Re-fetch current `main`, claims, and target blobs before implementation writes; never force-push.
- Record static/exact-diff/ancestry verification only unless a test runner is actually invoked.

## Coordination

Open-PR and current claim searches found no active reservation for `ProjectInterchangeJsonValidator` bounded file reading at registration time. Concurrent Grid/Wall and unrelated Core lanes remain out of scope.

## Completion condition

Current `main` enforces `MaxFileBytes` during file reading, focused regression coverage locks the boundary, and this claim is `COMPLETED`.
