# Work claim — QSDB primary semantic ID read canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-qsdb-primary-id-read-canonicality`
- Registered: `2026-08-11T23:54:00+07:00`
- Baseline main SHA: `4ec0e38a9bc0a331302a7fde6966da86d2773d9f`
- Priority: persisted semantic identity fail-open found during owner-requested continue-all audit

## Confirmed defect

Current-schema QSDB XML structure is validated before object materialization, but several primary semantic/key attributes are then read through constructors/helpers that trim them. A tampered current-schema file can therefore silently repair padded persisted identity such as project/Zone/Floor/Family/Element/QuantityRule ids, QuantityRule output names, or persisted quantity names. These are identity/key surfaces and should already be canonical in persisted current-schema state.

## Reserved scope

Require the following current-schema persisted attributes to be non-empty and free of leading/trailing whitespace before loader normalization:

- root `projectId`;
- Zone/Floor/Family/Element/QuantityRule `id`;
- QuantityRule `output`;
- quantity `<q name>`.

Do not change display names, formula expression/version text, drawing paths/fingerprints, arbitrary property values, audit text, or legacy migration algorithms.

## Expected surfaces

- `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs`
- `tests/QS3D.Core.SmokeTests/QsdbPrimaryIdCanonicalReadSmoke.cs`
- module-initializer registration in that new smoke file
- this claim file

## Excluded scope

- No relation/source-handle/dependency work; the immediately preceding lanes already own/complete those surfaces.
- No duplicate primary-ID algorithm change; post-materialization project validation already rejects duplicate semantic IDs/outputs.
- No schema-version bump or migration rewrite.
- No native BricsCAD lifecycle/SaveAs/runtime work.
- No GitHub Actions dispatch.

## Validation plan

- Padded projectId fails load.
- Padded Zone/Family/Element/QuantityRule ids fail load (representing catalog/element/rule primary-ID paths).
- Padded QuantityRule output and persisted quantity name fail load.
- Canonical primary/key values continue loading unchanged.
- Inspect exact implementation diff and read back current remote source/test; never force-push.

## Coordination

Recent QSDB map-key, relation/source canonicality and duplicate-list lanes are completed upstream. Search of recent commits found no separate primary-ID read-canonicality claim. Concurrent revision/UI/rebar/installer/export work is outside this scope.

## Completion condition

Current `main` rejects padded current-schema primary semantic/key identities before constructors/helpers can trim them, deterministic regression coverage is present, and this claim is closed `COMPLETED` with exact commits and actual validation scope.
