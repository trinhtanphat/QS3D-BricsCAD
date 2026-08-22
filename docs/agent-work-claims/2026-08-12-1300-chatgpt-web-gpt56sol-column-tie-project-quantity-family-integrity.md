# Work claim — Column tie project quantity family referential integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T13:00:00+07:00`
- Completed: `2026-08-12T14:40:00+07:00`
- Baseline main SHA: `a4abd6deb170c4332db72f659814b9852a6f764c`
- Priority: Concrete Core correctness defect: an unrelated Column family can silently supply fallback dimensions/rebar values for another Column element.

## Reserved scope

Require any supplied `ProjectFamily` in `ColumnTieProjectQuantityService.Calculate` to match the target `ProjectElement.FamilyId` before family fallback values are read.

## Expected surfaces

- `src/QS3D.Core/Rebar/ColumnTieProjectQuantityService.cs`
- `tests/QS3D.Core.SmokeTests/ColumnTieProjectQuantityFamilyIntegritySmoke.cs`
- `tests/QS3D.Core.SmokeTests/ColumnTieProjectQuantityFamilyIntegrityRegistration.cs` (module-initializer registration only)
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs` (reserved during coordination but intentionally left unchanged after confirming the module-initializer pattern)
- this claim file

## Completed contract

- Supplied families still must have `ElementCategory.Column`.
- A non-null supplied family must also match `element.FamilyId` with `StringComparison.OrdinalIgnoreCase` before any family fallback property is read.
- A mismatched Column family is rejected instead of silently supplying another element's Width/Depth/Height/Rebar fallback values.
- Matching family IDs remain case-insensitive, consistent with project identity lookup semantics.
- `family == null` remains supported; element-local values and existing defaults keep their previous behavior.
- Column tie geometry/math/spacing formulas and unrelated rebar services are unchanged.

## Evidence

- Initial claim registration: `c3856e7be20714a74ada42ed7cdc09f6b2703771`
- Registrar-surface coordination expansion: `b948a404ed8ea8168497c764d40fe3854a4df4c4`
- Module-initializer registration isolation: `7f0101deec3531ec565cbdb63c632adbe56765fa`
- Source fix: `8995de5f7fb41e4060c35693d1f758cd836aac71`
- Focused smoke: `0b8be37474b0e025f17784fcf8d6d6eaab46b935`
- Auto-registration: `172313fcb38d67bf27d7bee7ad03388c6a5bffb3`
- Main source blob after write: `366416c95c5d306343461df578208144d8b7b6d5`
- Main smoke blob after write: `af00932b9a7b2f04237d74495620c3224dbf00ce`
- Main registration blob after write: `86a25958d10d90b194469eb1041e9b057553ab3c`
- Read-back verified source, focused smoke, and module-initializer registration from current `main`.
- Post-write ancestry check: `172313fcb38d67bf27d7bee7ad03388c6a5bffb3` is the exact merge base of current-main snapshot `35393f4e939c856b853aa4cc6c934215fb762f7c`; `behind_by=0`.
- One earlier detached candidate commit `715e467f3171287399ae4ec61a30847f39cb1797` was not published because `update_ref(force=false)` correctly rejected a concurrent-main race; no force push was used.

## Validation boundary

Focused CAD-independent smoke source was added and auto-registered through the repository's existing `[ModuleInitializer]` pattern. GitHub Actions were not dispatched. A full .NET build, executable smoke process, and licensed BricsCAD V25/V26 runtime were not run and are not claimed as PASS.
