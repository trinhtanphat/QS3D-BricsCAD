# Work claim — generated solid null element health

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-generated-solid-null-element-health-20260812-0845`
- Registered: `2026-08-12T08:45:00+07:00`
- Baseline main SHA: `d8f69c5ab440e65e5943dda46ca83dc440d03e41`
- Priority: owner-requested continue-all residual runtime-health false-clean hardening

## Confirmed defect

`GeneratedSolidRuntimeHealthService.InspectGeneratedSolidOwnership(...)` executed `if (element == null) continue;`. A malformed `ProjectState` containing a null semantic element could therefore be partially inspected while the generated-solid ownership provider silently omitted the malformed entry. This conflicted with the fail-visible null-entry contract now used by sibling Core health services. The outer `AddProviderSafely(...)` boundary already converts recoverable provider exceptions into `RUNTIME_HEALTH_PROVIDER_FAILED`, so the provider can reject a null element without aborting aggregate runtime health.

## Reserved scope

- Make generated-solid ownership inspection reject null `project.Elements` entries instead of silently skipping them.
- Preserve provider isolation: the outer runtime-health aggregator must continue translating recoverable provider failures into diagnostics.
- Preserve all existing generated-solid empty-handle, malformed/unresolved/unreadable/erased/type/ownership diagnostics and strict read-only behavior.
- Add one focused static regression preflight for the null-element fail-visible contract.

## Implemented surfaces

- `src/QS3D.BricsCAD.V25/Cad/GeneratedSolidRuntimeHealthService.cs`
- `scripts/preflight-generated-solid-null-element-health.py`
- this claim file

## Integration evidence

- Claim registration: `e2a9354ef88f750fe2926cf106b40310757640fc`
- Source fix: `2243fd1470d4958cccbdb3e34f827a0b12be543b`
- Focused regression preflight: `6d18a2f86b714774750ce56b976ca2a3b2d43c7b`

## Validation performed

- Re-fetched the exact V25 source from current `main` after claim registration; the null-element silent skip remained present before editing.
- Source now throws a deterministic `InvalidOperationException` when generated-solid ownership inspection encounters a null semantic element instead of silently continuing.
- The outer `AddProviderSafely(...)` and `IsRecoverableDiagnosticFailure(...)` flow remains unchanged, so this recoverable provider failure is surfaced as `RUNTIME_HEALTH_PROVIDER_FAILED` while aggregate runtime health can continue with sibling providers.
- Re-fetched final source from current `main`; blob `cba3cc5e1e1f53b9f6b906751a9207a6dd511430` retains the null fail-visible guard, the previous empty-handle hardening, `OpenMode.ForRead`, and all existing generated-solid diagnostics.
- Re-fetched `scripts/preflight-generated-solid-null-element-health.py`; blob `0481d6beea5b150f6257fe4bcf65abb22dd368f8` rejects the old null skip and requires both the fail-visible guard and outer provider-isolation tokens.

## Validation boundary

Remote source/static readback only. This session did not execute the repository preflight process, a full .NET build/test, GitHub Actions, or licensed BricsCAD V25/V26 runtime. No native runtime, private-DWG, installer, signing or release PASS is claimed.

## Excluded scope

- No project persistence or null-entry repair changes.
- No sibling health/provider changes.
- No generated geometry mutation path changes.
- No GitHub Actions, release publication or force push.

## Completion condition

Satisfied on the source/static contract: current `main` no longer silently skips null semantic elements during generated-solid ownership health, provider isolation remains intact, focused regression source pins the contract, and exact integration evidence is recorded above.
