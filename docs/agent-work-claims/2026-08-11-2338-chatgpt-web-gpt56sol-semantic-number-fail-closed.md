# Work claim — semantic numeric fail-closed regeneration

- Status: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-semantic-number-fail-closed`
- Registered: `2026-08-11T23:38:00+07:00`
- Baseline main SHA: `0ab55e0e96e0a386bc76f5f8aedb432bf81fd43a`
- Priority: source-verifiable regeneration data-integrity defect found during owner-requested continue-all audit

## Confirmed defect

`SemanticNumber.Get(...)` returned its fallback whenever a numeric semantic property existed but could not be parsed as a finite invariant-culture number. This made malformed values such as `WidthM=abc`, `HeightM=NaN`, or a malformed optional/defaulted property indistinguishable from a genuinely missing property. Regenerators could therefore emit plausible zero/default quantities from corrupted semantic input instead of failing closed.

## Reserved scope

Change the shared CAD-independent semantic-number read contract so:

- a missing property still returns the supplied fallback exactly as before;
- a present property must parse as a finite invariant-culture `double`;
- malformed/non-finite present values throw before regeneration writes derived quantities.

Positivity, range, engineering, Level, geometry and category policy remain owned by `QuantityMath` and the individual regenerators/planners.

## Expected surfaces

- `src/QS3D.Core/Services/SemanticRegenerators.cs` (`SemanticNumber.Get` only)
- `tests/QS3D.Core.SmokeTests/SemanticNumberFailClosedSmoke.cs`
- this claim file

## Excluded scope

- No `ProjectElement.SetProperty` schema/type redesign.
- No native BricsCAD source capture, UI validation, Direct Draw, modeless lifecycle or V25 runtime work.
- No changes to numeric sign/range policy or fabrication/engineering rules.
- No unrelated Structural/Wall/Room/Opening algorithm rewrite.
- No GitHub Actions dispatch.

## Delivered behavior

- Missing semantic numeric properties preserve their existing fallback behavior.
- Present malformed or non-finite semantic numeric properties now throw `InvalidOperationException` rather than silently falling back.
- This applies consistently to the shared numeric reads used by Wall/Opening/Room and Structural regenerators without changing their later positivity/range algorithms.
- Existing valid finite values remain unchanged.

## Commits

- Registration: `8ed1c2fcf33f6856510fc1eb2046bb0c53388cbb` — `chore(agent): claim semantic numeric fail-closed regeneration`.
- Implementation: `050fc74ed63ebfdd946454b96f13234a0a17476c` — `fix(regen): fail closed on malformed semantic numbers`.
- Regression: `90d367247fdd94cec7e0a05f4a2fcbbd521bfb3a` — `test(regen): guard malformed semantic numbers`.

## Validation actually performed

- Inspected the exact implementation commit diff: only `SemanticNumber.Get` changed in the shared source file; no accidental rewrite of neighboring regenerators occurred.
- Re-fetched current remote source and confirmed present malformed/non-finite values throw while missing values return the fallback.
- Re-fetched the focused smoke from current `main`; it covers malformed Opening width, non-finite Opening height, missing optional Railing fallbacks, malformed optional Railing height, and unchanged valid Opening calculations.
- Focused smoke auto-registers with a module initializer and does not modify the shared smoke registration file.
- No force-push was used and concurrent source-reconcile/installer/UI/export work remained intact.
- No GitHub Actions were dispatched.
- This hosted environment has no local .NET SDK/compiler and no licensed BricsCAD V25 runtime, so no unexecuted build/runtime PASS is claimed. This is a Core-only source contract and does not add a new native runtime scenario.

## Completion condition

Satisfied: current `main` distinguishes missing numeric properties from malformed/non-finite present properties, deterministic regression coverage is present, and this claim is closed as `COMPLETED`.
