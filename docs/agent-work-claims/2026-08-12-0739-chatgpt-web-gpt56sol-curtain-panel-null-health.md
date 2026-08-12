# Work claim — Curtain Panel health null-element fail-visible

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-curtain-panel-null-health-20260812-0739`
- Registered: `2026-08-12T07:39:00+07:00`
- Baseline main SHA: `4cea54ffa1cbc5fffe1c1a6f62759beea69aba09`
- Priority: diagnostic integrity during owner-requested `continue all`

## Confirmed defect

`GeneratedCurtainPanelHealthService.Inspect(ProjectState, ...)` silently skips null semantic elements with `if (element == null) continue;`. This specialized provider can therefore return a false-clean result for malformed project state instead of participating in the fail-visible provider contract used by `ComprehensiveModelHealthService.AddSafely(...)`, which converts diagnostic data failures into Error-level `HEALTH_PROVIDER_FAILED` issues.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedCurtainPanelHealthService.cs` — null semantic-element handling only
- `tests/QS3D.Core.SmokeTests/GeneratedCurtainPanelNullHealthSmoke.cs` — focused Core regression
- `tests/QS3D.Core.SmokeTests/GeneratedCurtainPanelNullHealthSmokeRegistration.cs` — isolated module-initializer registration
- this claim file for close-out

## Contract

- direct Curtain Panel health inspection rejects a project containing a null semantic element;
- comprehensive health surfaces the provider failure as Error-level `HEALTH_PROVIDER_FAILED` naming `GeneratedCurtainPanelHealthService`;
- valid empty-project Curtain Panel diagnostics remain empty;
- existing panel handle/build-state/count/fingerprint/mode/category/stale diagnostics remain unchanged.

## Coordination / exclusions

Recent null-health claims for Generated Rebar Mode, Foundation Mesh, Semantic Tag and Audit Trail use different providers/files. No current commit search found a Curtain Panel null-health claim. No Curtain geometry/generation, ownership policy, persistence, UI/native BricsCAD, release/update, or unrelated health behavior changes.

No GitHub Actions dispatch and no BricsCAD V25/V26 runtime PASS claim from this connector session.
