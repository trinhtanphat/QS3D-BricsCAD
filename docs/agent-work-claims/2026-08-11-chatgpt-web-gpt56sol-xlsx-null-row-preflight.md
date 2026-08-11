# Agent Work Claim — XLSX null-row preflight

- Agent: ChatGPT Web / GPT-5.6 Sol
- Date: 2026-08-11
- Status: ACTIVE
- Branch/target: direct `main` under current `AGENTS.md` coordination policy

## Scope reservation

This claim reserves only the following source-safe export/test lane:

- `src/QS3D.Core/Export/DoorOpeningXlsxExporter.cs`
- `src/QS3D.Core/Export/MaterialUsageXlsxExporter.cs`
- `src/QS3D.Core/Export/CurtainWallXlsxExporter.cs`
- `src/QS3D.Core/Export/RoomFinishXlsxExporter.cs`
- `tests/QS3D.Core.SmokeTests/XlsxScheduleNullRowSmoke.cs` (new)
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs` (shared registry; re-read immediately before update)
- this claim file

Do not overlap unrelated ProjectInterchange, BricsCAD command/UI, Room Finish regeneration, Reporting provenance, persistence, or other active-agent lanes.

## Verified defect

All four public schedule XLSX exporters validate only `rows != null`. A caller can still pass an `IReadOnlyList<T>` containing a null row. The exporters then create a temp XLSX package and dereference that row inside `BuildSheet`, producing an incidental `NullReferenceException` rather than a fail-fast argument contract. The destination is currently protected by atomic replacement, but invalid input still does unnecessary filesystem/package work and exposes inconsistent failure semantics.

## Implementation plan

1. Add a fail-fast row preflight in each exporter immediately after the existing null-list check and before path normalization/directory/temp-file creation.
2. Reject the first null row with `ArgumentException`, include the zero-based row index in the message, and bind the exception to `rows`.
3. Add a Core-only regression smoke covering all four exporters. Each case must prove invalid null-row input fails before replacing an existing destination sentinel and that the message identifies the bad row index.
4. Register the smoke through `SmokeTestRegistration.cs` after re-reading the latest shared registry.
5. Re-read final `main`, inspect commit status/workflow evidence, and close this claim with exact commit SHAs. Do not report `LOCAL_PASS` unless actually executed in an appropriate environment.

## Validation boundary

This is CAD-independent Core code and does not require BricsCAD V25/DWG UI proof. Runtime smoke execution will be attempted only if the available environment can obtain/build the repository; otherwise source integration and CI/status evidence will be reported precisely without inventing a pass.
