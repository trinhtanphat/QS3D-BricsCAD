# Work claim — Curtain Panel health null-element fail-visible

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-curtain-panel-null-health-20260812-0739`
- Registered: `2026-08-12T07:39:00+07:00`
- Completed: `2026-08-12T07:41:00+07:00`
- Baseline main SHA: `4cea54ffa1cbc5fffe1c1a6f62759beea69aba09`
- Reservation commit: `b7055577f345b8f190face644cfd91eda6293444`
- Priority: diagnostic integrity during owner-requested `continue all`

## Defect fixed

`GeneratedCurtainPanelHealthService.Inspect(ProjectState, ...)` previously skipped null semantic elements and could return a false-clean result for malformed project state. It now throws `InvalidOperationException` on a null semantic element, matching the repository's fail-visible specialized-health contract. `ComprehensiveModelHealthService.AddSafely(...)` converts that bounded diagnostic data failure into an Error-level `HEALTH_PROVIDER_FAILED` issue naming this provider.

## Published commits

- `04fd1c0ded6c166bb6d077584076a118c6fc398e` — `fix(health): fail visible on null curtain panel entries`
- `14de4e113f653a99a2a7278279574b667da7a304` — `test(health): guard null curtain panel diagnostics`
- `a3376d05d0026e0beb7c99f23d4869b474e4c90d` — `test(health): register curtain panel null smoke`

## Preserved contract / regression evidence

- direct Curtain Panel health inspection rejects a project containing a null semantic element;
- comprehensive health is expected to surface the provider failure as Error-level `HEALTH_PROVIDER_FAILED` naming `GeneratedCurtainPanelHealthService` through its existing wrapper;
- valid empty-project Curtain Panel diagnostics remain empty;
- existing panel handle/build-state/count/fingerprint/mode/category/stale diagnostics were not changed;
- focused smoke covers the direct provider, comprehensive wrapper visibility, and empty-project healthy path;
- smoke registration uses the existing `[ModuleInitializer]` convention without editing the shared runner.

## Coordination / validation boundaries

Recent null-health claims for Generated Rebar Mode, Foundation Mesh, Slab Mesh, Semantic Tag and Audit Trail use different providers/files. No Curtain geometry/generation, ownership policy, persistence, UI/native BricsCAD, release/update, or unrelated health behavior changed. Source/test writes used GitHub current-blob/path operations and no force-push.

Connector validation inspected source/test/registration and repository ancestry only; no local `dotnet`/Core smoke execution is claimed. No GitHub Actions were dispatched and no BricsCAD V25/V26 runtime PASS is claimed.
