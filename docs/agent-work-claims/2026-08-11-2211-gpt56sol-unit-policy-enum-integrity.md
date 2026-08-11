# Work claim — quantity-unit binding source enum integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-unit-policy-enum-integrity-20260811-2211`
- Registered: `2026-08-11T22:11:28+07:00`
- Scope amended: `2026-08-11T22:16:00+07:00`
- Baseline main SHA: `10438bbc3b2c9e6ba53011d37cac3c2bf2e3f65e`
- Priority: evidence-driven Core invariant hardening during owner-requested `continue all`

## Reserved scope

Harden the CAD-independent quantity-unit binding boundary so an undefined `DrawingUnitResolutionSource` cannot be accepted and persisted into project metadata.

## Expected surfaces

- `src/QS3D.Core/Units/DrawingUnitResolutionPolicy.cs`
- `tests/QS3D.Core.SmokeTests/DrawingUnitBindingSourceIntegritySmoke.cs`
- `tests/QS3D.Core.SmokeTests/DrawingUnitBindingSourceIntegritySmokeRegistration.cs`
- this claim file for close-out

## Coordination amendment

After this claim was published, ancestry review exposed an earlier reservation commit `4a993ce9e9ebaef9d6aad552ac93173210416f6e` (`chore(agent): claim project unit policy enum integrity`) registered at `2026-08-11T22:06:48+07:00`. That earlier claim owns `src/QS3D.Core/Units/ProjectUnitPolicy.cs` and `tests/QS3D.Core.SmokeTests/DrawingUnitResolutionSmoke.cs`, and its implementation `9336938914be2963ad0a780f65ea61c9ecf7dda2` has already added constructor validation.

This claim therefore **releases and excludes** both overlapping surfaces. It also avoids shared `Program.cs` / `SmokeTestRegistration.cs` by using the repository's current focused smoke + `ModuleInitializer` registration pattern. The remaining defect is independent: public `BindQuantityUnit(...)` accepts any numeric `DrawingUnitResolutionSource` and serializes `source.ToString()` into `QS3D.DrawingUnitBindingSource.v1`.

## Explicit exclusions

- No `ProjectUnitPolicy` changes and no edits to the earlier agent's unit smoke.
- No shared smoke-runner file edits.
- No BricsCAD V25 adapter/runtime/UI changes.
- No `QS3DUNITS` command lifecycle, project-context, save/reopen, or LOCAL-001 qualification changes.
- No unit conversion-factor changes or INSUNITS mapping expansion.
- No updater/licensing, Build3D, Xref, rebar, health dependency, documentation-editor, persistence/interchange, or other currently claimed lanes.
- No GitHub Actions dispatch or release work.

## Validation plan

- `DrawingUnitResolutionPolicy.BindQuantityUnit` rejects undefined `DrawingUnitResolutionSource` before compatibility lookup or metadata mutation.
- Focused Core smoke verifies rejection and verifies the supplied metadata remains unchanged.
- Focused Core smoke also preserves one valid binding assertion so the guard cannot reject legitimate source values.
- Re-fetch current `main` before the coherent implementation commit, preserve concurrent changes, then re-read the pushed source/test from current `main`.

## Completion condition

The invalid binding-source enum fails at the public boundary without partial state, focused regression coverage is present on current `main`, and this claim is marked `COMPLETED` with the exact implementation/final SHA and validation actually performed.
