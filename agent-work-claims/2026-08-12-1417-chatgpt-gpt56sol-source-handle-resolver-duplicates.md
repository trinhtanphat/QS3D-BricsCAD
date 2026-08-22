# Work claim — SourceHandleResolver duplicate SourceHandles

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-source-handle-resolver-duplicates-20260812-1417`
- Registered: `2026-08-12T14:17:00+07:00`
- Erroneously cancelled: `2026-08-12T14:46:00+07:00`
- Reactivated: `2026-08-12T14:47:00+07:00`
- Completed: `2026-08-12T14:50:00+07:00`
- Priority: P1 ownership integrity parity

## Confirmed defect

The production resolver is `src/QS3D.Core/Services/SourceHandleResolver.cs`. Its `AddDirectHandles()` validated blank/non-canonical entries but merged every direct source handle directly into one traversal-wide case-insensitive `knownHandles` set. Therefore duplicate `ProjectElement.SourceHandles` entries in the same element, including case aliases such as `ABCD` + `abcd`, were silently deduplicated instead of failing closed as malformed ownership data.

## Cancellation correction

Commit `131b13a7fd13717132bfa9507f1b133c48d8c3d7` cancelled this claim after checking the stale/nonexistent path `src/QS3D.Core/Rooms/SourceHandleResolver.cs`. Git history and PR #784 identify the actual production path as `src/QS3D.Core/Services/SourceHandleResolver.cs`; direct `main` readback confirmed the defect there. The cancellation premise was invalid, so the original claim was reactivated and completed rather than replaced by a new overlapping lane.

## Implemented contract

`AddDirectHandles()` now tracks handle-to-first-index identity per semantic element with `StringComparer.OrdinalIgnoreCase`. Exact duplicates and case aliases fail closed before traversal-wide merge, and the exception identifies both the first and current duplicate indices. The existing traversal-wide `knownHandles` set remains unchanged, preserving cross-element deduplication, direct/boundary/generated precedence, dependency traversal and deterministic ordering.

## Regression coverage

The existing auto-registered `tests/QS3D.Core.SmokeTests/SourceHandleResolverSafetySmoke.cs` now covers:

- exact duplicate `ABCD` + `ABCD` rejection;
- case-alias duplicate `ABCD` + `abcd` rejection;
- first/current duplicate index diagnostics (`0` and `1`);
- a unique two-handle control preserving direct resolution order.

## Landing evidence

- Claim reactivation/correction: `dcbc29e4c501ef733968da7dc9fcb668a74eccc4`
- Source fix: `eeae034a69087b959caeeb3b8ad2dd1969763137`
- Source blob read back from `main`: `e0438f53477437141ccde29907ec12175c603eef`
- Regression smoke: `49fb06b1cc65852bb4a4073753769aa2f30d1eeb`
- Smoke blob read back from `main`: `5f5765a88cfb880c7040a1113daafed1148c27b5`
- `main` readback after source + smoke: `49fb06b1cc65852bb4a4073753769aa2f30d1eeb`

## Validation boundary

Remote source/smoke readback confirms the intended implementation and regression are present on `main`. No GitHub Actions/full build/licensed BricsCAD runtime was executed for this lane, so no executable runtime PASS is claimed.
