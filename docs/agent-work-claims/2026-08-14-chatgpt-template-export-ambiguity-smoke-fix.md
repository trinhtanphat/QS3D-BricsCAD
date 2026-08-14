# Work claim — Template export ambiguity smoke fixture

- Status: `ACTIVE`
- Agent: `chatgpt/github-integration`
- Registered: `2026-08-14T18:04:00+07:00`
- Baseline main SHA: `c7332a5a273ad5d58b8853e2b03d42d2486a3f3b`
- Implementation branch: `agent/chatgpt/template-export-ambiguity-smoke-20260814`
- Integration batch: `integration/20260814-v25-cloud-final`
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

## Completion condition

The corrected smoke fixture is integrated into current `main`, a new V25 cloud workflow run (new run number and run ID, attempt 1) starts from the resulting integration landing, and this claim is closed with exact evidence.
