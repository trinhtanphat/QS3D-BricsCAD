# Work claim — Project interchange bounded file read

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:28:00+07:00`
- Completed: `2026-08-12T10:52:00+07:00`
- Baseline main SHA: `d030e1655075b4055cdf46fc451f27b9c58b5a7a`
- Source merge SHA: `4fa8d52221981f27185bd59df09ec6a694d76c58`
- Source PR: `#783`
- Priority: evidence-driven remote-safe interchange input hardening

## Reason

`ProjectInterchangeJsonValidator.ValidateFile()` checked `FileInfo.Length` against the existing 16 MiB `MaxFileBytes` contract, then performed an unbounded `File.ReadAllBytes(fullPath)`. A file that grew after the metadata check could therefore be read/allocated beyond the guarded size before the later string validation rejected it.

## Reserved scope

Make the file-read boundary enforce the existing `MaxFileBytes` contract while bytes are read. Preserve path validation, not-found behavior, strict UTF-8 handling, validation result semantics, the exact 16 MiB public limit, JSON limits, and exporter/importer behavior.

## Completed surfaces

- `src/QS3D.Core/Export/ProjectInterchangeJsonValidator.cs`
- `tests/QS3D.Core.SmokeTests/ProjectInterchangeValidationSmoke.cs`
- `scripts/preflight-interchange-bounded-file-read.py`
- this claim file

## Completion evidence

- PR `#783` squash-merged to `main` as `4fa8d52221981f27185bd59df09ec6a694d76c58`.
- `ValidateFile()` now calls `ReadFileBytesBounded(fullPath)` instead of `File.ReadAllBytes(fullPath)`.
- The bounded reader buffers at most `MaxFileBytes` and probes one additional byte with `ReadByte()` to reject post-check growth beyond the 16 MiB contract.
- The existing strict UTF-8 decode path and oversize `InvalidDataException` text remain unchanged.
- `ProjectInterchangeValidationSmoke` now locks the public oversize limit/error contract.
- `scripts/preflight-interchange-bounded-file-read.py` fails if the bounded helper/sentinel contract is removed or `File.ReadAllBytes(fullPath)` returns.
- Readback was performed against source merge `4fa8d52221981f27185bd59df09ec6a694d76c58`.
- GitHub Actions were not dispatched. No full build/smoke suite or BricsCAD runtime PASS is claimed by this remote lane.

## Excluded scope

- No interchange schema/version or collection-limit changes.
- No exporter/import planner changes.
- No BricsCAD command/UI changes.
- No GitHub Actions dispatch.

## Coordination

The concurrent LOCAL-003 interchange UTC fixture lane was scoped to `ProjectInterchangeExportSafetySmoke.cs` and did not overlap this validator/smoke/preflight implementation.

## Completion condition

Satisfied: current `main` enforces `MaxFileBytes` during file reading, focused regression/preflight coverage locks the boundary, and this claim is `COMPLETED`.
