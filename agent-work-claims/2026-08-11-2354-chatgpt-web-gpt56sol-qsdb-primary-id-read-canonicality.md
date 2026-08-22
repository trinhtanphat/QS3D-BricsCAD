# Work claim — QSDB primary semantic ID read canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-qsdb-primary-id-read-canonicality`
- Registered: `2026-08-11T23:54:00+07:00`
- Baseline main SHA: `4ec0e38a9bc0a331302a7fde6966da86d2773d9f`
- Priority: persisted semantic identity fail-open found during owner-requested continue-all audit

## Confirmed defect

Current-schema QSDB XML structure was validated before object materialization, but several primary semantic/key attributes were then read through constructors/helpers that trim them. A tampered current-schema file could therefore silently repair padded persisted identity such as project/Zone/Floor/Family/Element/QuantityRule ids, QuantityRule output names, or persisted quantity names. These are identity/key surfaces and must already be canonical in persisted current-schema state.

## Reserved scope

Require the following current-schema persisted attributes to be non-empty and free of leading/trailing whitespace before loader normalization:

- root `projectId`;
- Zone/Floor/Family/Element/QuantityRule `id`;
- QuantityRule `output`;
- quantity `<q name>`.

Display names, formula expression/version text, drawing paths/fingerprints, arbitrary property values, audit text and migration algorithms remain unchanged.

## Expected surfaces

- `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs`
- `tests/QS3D.Core.SmokeTests/QsdbPrimaryIdCanonicalReadSmoke.cs`
- this claim file

## Excluded scope

- No relation/source-handle/dependency work; preceding lanes completed those surfaces.
- No duplicate primary-ID algorithm change; post-materialization project validation continues handling duplicate semantic IDs/outputs.
- No schema-version bump or migration rewrite.
- No native BricsCAD lifecycle/SaveAs/runtime work.
- No GitHub Actions dispatch.

## Delivered behavior

- Current-schema project, Zone, Floor, Family, Element and QuantityRule ids must already be canonical before object constructors can trim them.
- QuantityRule output identity and persisted quantity names receive the same required canonical check.
- Existing canonical values load unchanged.
- No display/content fields were made whitespace-strict by this lane.

## Commits

- Registration: `d22fdee7428e65aa6c91f172c5d0915c56308e9b` — `chore(agent): claim qsdb primary id read canonicality`.
- Implementation: `f017febf1592f9951b9595d24d17bc365ac09edc` — `fix(persistence): reject padded persisted primary ids`.
- Regression: `7923b6bb20983225b0063d1e272354e2864e327c` — `test(persistence): guard persisted primary id canonicality`.

## Validation actually performed

- Inspected the exact implementation diff; it only adds required-canonical attribute checks for the reserved identity/key surfaces.
- Re-fetched the focused smoke from current remote `main`; it covers padded project, Zone, Family, Element and QuantityRule ids, padded rule output, padded quantity name, plus unchanged canonical loading.
- The smoke auto-registers with a module initializer and does not modify the shared smoke registration file.
- No force-push was used; unrelated concurrent commits remained intact.
- No GitHub Actions were dispatched.
- This hosted environment has no local .NET SDK/compiler and no licensed BricsCAD V25 runtime, so no unexecuted build/runtime PASS is claimed. This persistence/Core hardening does not introduce a new native runtime scenario.

## Completion condition

Satisfied: current `main` rejects padded current-schema primary semantic/key identities before constructors/helpers can trim them, deterministic regression coverage is present, and this claim is closed `COMPLETED`.
