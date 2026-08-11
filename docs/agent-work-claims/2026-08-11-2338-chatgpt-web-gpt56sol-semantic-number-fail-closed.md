# Work claim — semantic numeric fail-closed regeneration

- Status: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-semantic-number-fail-closed`
- Registered: `2026-08-11T23:38:00+07:00`
- Baseline main SHA: `0ab55e0e96e0a386bc76f5f8aedb432bf81fd43a`
- Priority: source-verifiable regeneration data-integrity defect found during owner-requested continue-all audit

## Confirmed defect

`SemanticNumber.Get(...)` currently returns its fallback whenever a numeric semantic property exists but cannot be parsed as a finite invariant-culture number. This makes malformed values such as `WidthM=abc`, `HeightM=NaN`, or `CurtainMaxPanelWidthM=Infinity` indistinguishable from a genuinely missing optional property. Regenerators can therefore emit plausible zero/default quantities from corrupted semantic input instead of failing closed.

## Reserved scope

Change the shared CAD-independent semantic-number read contract so:

- a missing property still returns the supplied fallback exactly as today;
- a present property must parse as a finite invariant-culture `double`;
- malformed/non-finite present values throw before regeneration writes derived quantities.

Do not change positivity, range, engineering, Level, geometry or category policy; those remain owned by `QuantityMath` and the individual regenerators/planners.

## Expected surfaces

- `src/QS3D.Core/Services/SemanticRegenerators.cs` (`SemanticNumber.Get` only)
- `tests/QS3D.Core.SmokeTests/SemanticNumberFailClosedSmoke.cs`
- module-initializer registration in that new smoke file
- this claim file

## Excluded scope

- No `ProjectElement.SetProperty` schema/type redesign.
- No native BricsCAD source capture, UI validation, Direct Draw, modeless lifecycle or V25 runtime work.
- No changes to numeric sign/range policy or fabrication/engineering rules.
- No unrelated Structural/Wall/Room/Opening algorithm rewrite.
- No GitHub Actions dispatch.

## Validation plan

- Present malformed Opening `WidthM` fails before writing `OpeningAreaM2`.
- Present non-finite numeric property fails closed.
- Missing numeric property still uses the legacy fallback contract.
- Valid finite values regenerate exactly as before.
- A present malformed optional/defaulted property fails rather than silently using the default.
- Focused smoke auto-registers without changing shared `SmokeTestRegistration.cs`.
- Re-fetch current target blob after the claim commit and read back final source/test from current `main`; never force-push.

## Coordination

Current concurrent reservations observed around source reconcile audit revision, generated rebar audit touch, installer/uninstall, quantity locate/UI, worksheet bounds and other unrelated surfaces. No current/recent claim was found for `SemanticNumber` or malformed semantic numeric regeneration.

## Completion condition

Current `main` distinguishes missing numeric properties from malformed/non-finite present properties, deterministic regression coverage is present, and this claim is closed as `COMPLETED` with exact commits and validation actually performed.
