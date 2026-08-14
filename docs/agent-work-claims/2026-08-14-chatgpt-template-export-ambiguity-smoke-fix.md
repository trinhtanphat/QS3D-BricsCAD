# Work claim — Template export ambiguity smoke fixture

- Status: `COMPLETED`
- Agent: `chatgpt/github-integration`
- Registered: `2026-08-14T18:04:00+07:00`
- Baseline main SHA: `c7332a5a273ad5d58b8853e2b03d42d2486a3f3b`
- Implementation branch: `agent/chatgpt/template-export-ambiguity-smoke-20260814`
- Implementation commit: `39948a38a090ba9846f373ff7f7640e716b5b8f1`
- Integration batch: `integration/20260814-v25-cloud-final`
- Final integration landing: `eaeef35d1d4a6c0827bec3eaed278d2eb91dbe89`
- Fresh V25 cloud run: `#171` / run `31795377920`, attempt `1`, source integration SHA `eaeef35d1d4a6c0827bec3eaed278d2eb91dbe89`
- Priority: fresh V25 cloud run #170 failed at `TemplateExportLayerMappingAmbiguitySmoke.AmbiguousMappingsFailClosed`.

## Reserved scope

Reconcile the stale template-export ambiguity smoke fixture with the canonical normalized-ambiguity contract already used by the template-apply smoke, without weakening production mapping validation.

## Expected surfaces

- `tests/QS3D.Core.SmokeTests/TemplateExportLayerMappingAmbiguitySmoke.cs`
- no production source changes unless the current contract proves incorrect.

## Excluded scope

- unrelated historical agent branches;
- LOCAL_ONLY BricsCAD runtime gates;
- arbitrary CI/workflow changes;
- weakening or skipping smoke assertions.

## Validation plan

- preserve fail-closed export assertion and read-only project-state assertion;
- use two canonical patterns that normalize to the same recognition key (`A-WALL` and `A WALL`), matching the already-correct template-apply ambiguity fixture;
- integrate through `integration/20260814-v25-cloud-final`, land once to `main`, and verify a brand-new `release-v25-cloud.yml` run is created.

## Completion evidence

- The implementation branch commit `39948a38a090ba9846f373ff7f7640e716b5b8f1` changed only the stale template-export smoke fixture from `A-WALL` + ` A-WALL ` to the canonical normalized ambiguity pair `A-WALL` + `A WALL`.
- The combined integration landing `eaeef35d1d4a6c0827bec3eaed278d2eb91dbe89` contains that exact tree change on `main`.
- The automatic post-integration dispatcher created brand-new V25 cloud run `#171` (`31795377920`, attempt `1`) from integration SHA `eaeef35d1d4a6c0827bec3eaed278d2eb91dbe89`.
- Release preparation subsequently advanced `main` to the bot-owned version bump for preview `10002`; that release-preparation push is intentionally excluded from recursive dispatch by CI policy and does not invalidate the integration evidence above.

## Completion condition

Satisfied. The corrected smoke fixture is integrated, the fresh run started from the integration landing, and this claim is closed with exact branch, implementation, integration and workflow evidence.
