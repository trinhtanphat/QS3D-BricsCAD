# Work claim — Curtain Frame geometry snapshot canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-curtain-frame-geometry-snapshot-canonicality`
- Registered: `2026-08-12T11:05:00+07:00`
- Baseline main SHA: `8ced6f932a3e5a7e3618116587e0363e72ea136b`
- Priority: P1 — generated Curtain Frame geometry snapshots must preserve exact writer-owned round-trip numeric spelling.
- Task Key: `CORE-CURTAIN-FRAME-GEOMETRY-SNAPSHOT-CANONICALITY`

## Confirmed defect

Both `CurtainWallFrameSolidBuilder` and `CurtainWallPathFrameSolidBuilder` persist `GeneratedCurtainFrameDepthM`, `GeneratedCurtainFrameSourceLengthM`, and `GeneratedCurtainFrameHeightM` with `double.ToString("R", CultureInfo.InvariantCulture)`. `GeneratedCurtainFrameHealthService` currently validates these snapshots through numeric parsing only, so alternate raw spellings for the same positive value can pass health even though the writers never emit those spellings.

## Non-overlap check

Recent claim/commit search found no Curtain Frame numeric/sizing/geometry-snapshot canonicality lane. Completed Curtain Frame handle, mode and source-kind canonicality lanes own different metadata. Other active Curtain/Reporting/Revision/XLSX lanes do not reserve these three generated geometry snapshots.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs`
- one focused Core smoke regression for the three generated geometry snapshots
- this claim file

Do not modify Curtain Frame builders, current semantic geometry inputs, handles/counts/mode/source-kind/fingerprint metadata, native ownership/XData, persistence format, command wrappers, or BricsCAD runtime code.

## Intended contract

- After a stored snapshot parses as finite and positive, its raw text must equal `value.ToString("R", CultureInfo.InvariantCulture)` or emit a dedicated `HealthSeverity.Error` canonicality diagnostic.
- Existing invalid/nonfinite/nonpositive warnings retain precedence; invalid values do not receive canonicality noise.
- Existing geometry stale comparisons continue to use parsed numeric values.
- Exact writer-owned round-trip strings preserve existing behavior.
- Inspection remains read-only and deterministic.

## Completion condition

Alternate raw spellings for depth/source-length/height are fail-visible without changing invalid/stale semantics, focused smoke coverage pins aliases plus invalid/stale/canonical controls, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
