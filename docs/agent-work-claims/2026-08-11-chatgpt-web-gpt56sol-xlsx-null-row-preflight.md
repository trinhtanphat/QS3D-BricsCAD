# Agent Work Claim — XLSX null-row preflight

- Agent: ChatGPT Web / GPT-5.6 Sol
- Date: 2026-08-11
- Status: COMPLETE
- Branch/target: direct `main` under current `AGENTS.md` coordination policy

## Scope reservation

This claim reserved only the following source-safe export/test lane:

- `src/QS3D.Core/Export/DoorOpeningXlsxExporter.cs`
- `src/QS3D.Core/Export/MaterialUsageXlsxExporter.cs`
- `src/QS3D.Core/Export/CurtainWallXlsxExporter.cs`
- `src/QS3D.Core/Export/RoomFinishXlsxExporter.cs`
- `tests/QS3D.Core.SmokeTests/XlsxScheduleNullRowSmoke.cs`
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs` (shared registry; re-read immediately before update)
- this claim file

No unrelated ProjectInterchange, BricsCAD command/UI, Room Finish regeneration, Reporting provenance, persistence, or other active-agent lane was modified.

## Verified defect

All four public schedule XLSX exporters validated only `rows != null`. A caller could still pass an `IReadOnlyList<T>` containing a null row. The exporters then reached path/filesystem/temp-package work and eventually dereferenced that row inside `BuildSheet`, producing an incidental `NullReferenceException` rather than a fail-fast argument contract.

## Implementation completed

1. Each exporter now scans rows immediately after the existing null-list check and before `Path.GetFullPath`, directory creation, temp-file creation, or package writing.
2. The first null row is rejected with `ArgumentException`, `ParamName == "rows"`, and a message containing its zero-based row index.
3. `XlsxScheduleNullRowSmoke` exercises Door Opening, Material Usage, Curtain Wall, and Room Finish exporters. For every exporter it verifies the exception contract, preserves an existing destination sentinel, and verifies a missing output directory is not created before failure.
4. The smoke is registered in the shared smoke registry after re-reading the latest registry blob.

## Commits on `main`

- Claim registration: `67b16bbdbac00257787c7fb1e4f064c8c3cfa9f7`
- Door Opening exporter: `ab6e52dba265da8a3432511f0955e8df787cd13c`
- Material Usage exporter: `91b2fa229159e6951851a149af828cad8e00bfd3`
- Curtain Wall exporter: `8c46e2b945a2fc855862add45cafcb84c5d44a73`
- Room Finish exporter: `4de307dd4fb8afa914bdaccf44bc9ac43452de45`
- Regression smoke: `ad8298f4c171620a11cdb059f434cfa4e4f4a5be`
- Smoke registry: `da32b3b514a861f389aff006d4dce7da0be8352e`

## Validation evidence

- Re-read current `main` after the writes: all four exporters contain the same fail-fast preflight before filesystem work.
- Re-read `XlsxScheduleNullRowSmoke.cs`: all four exporters are covered and the filesystem/sentinel checks are present.
- Re-read `SmokeTestRegistration.cs`: `XlsxScheduleNullRowSmoke.Run();` is present.
- `get_commit_combined_status` for `da32b3b514a861f389aff006d4dce7da0be8352e` returned no automatic statuses/checks; no CI pass is claimed.
- Runtime smoke execution was attempted by cloning the repository in the available container, but the environment failed before checkout with `Could not resolve host: github.com`. No runtime pass or `LOCAL_PASS` is claimed.
- No BricsCAD V25/DWG validation is required for this CAD-independent Core export contract.
