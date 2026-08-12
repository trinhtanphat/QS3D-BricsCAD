# Work claim — Column tie project quantity family referential integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T13:00:00+07:00`
- Baseline main SHA: `a4abd6deb170c4332db72f659814b9852a6f764c`
- Priority: Concrete Core correctness defect: an unrelated Column family can silently supply fallback dimensions/rebar values for another Column element.

## Reserved scope

Require any supplied `ProjectFamily` in `ColumnTieProjectQuantityService.Calculate` to match the target `ProjectElement.FamilyId` before family fallback values are read.

## Expected surfaces

- `src/QS3D.Core/Rebar/ColumnTieProjectQuantityService.cs`
- `tests/QS3D.Core.SmokeTests/ColumnTieProjectQuantityFamilyIntegritySmoke.cs`
- `tests/QS3D.Core.SmokeTests/ColumnTieProjectQuantityFamilyIntegrityRegistration.cs` (module-initializer registration only)
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs` (reserved during coordination; no edit intended after confirming the module-initializer pattern)
- this claim file

## Excluded scope

- Column tie geometry/math/spacing formulas.
- Rebar notation/schedule grammar.
- Other rebar planners or project quantity services.
- Native BricsCAD materialization/runtime qualification.
- Code/standard-specific fabrication behavior.

## Validation plan

- Focused CAD-independent auto-registered smoke: mismatched family is refused; matching family fallback still calculates case-insensitively; null-family/element-only input remains supported.
- Re-fetch implementation diff, source, smoke, and module-initializer registration after writes.
- No GitHub Actions dispatch and no BricsCAD V25/V26 or compiled-test PASS claim from this remote lane.

## Coordination

No current claim/commit was found for this supplied-family referential-integrity lane after repeated current-main refreshes. A repo-local `[ModuleInitializer]` registration pattern was confirmed, so the focused smoke will use its own registration file and will not modify central `SmokeTestRegistration.cs`. Refresh `main` before every write and stop/reconcile if a new overlapping reservation appears.

## Completion condition

Source fix and focused regression are pushed to `main`, and this claim is marked `COMPLETED` with exact commit SHAs and validation limits.
