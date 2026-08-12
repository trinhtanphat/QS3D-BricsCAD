# Work claim — runtime health null provider issue

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-runtime-health-null-provider-issue-20260812-0847`
- Registered: `2026-08-12T08:47:00+07:00`
- Baseline main SHA: `98469144f23aa55c3a3b715316247138ea73fad2`
- Priority: owner-requested continue-all native runtime-health false-clean hardening

## Confirmed defect

`GeneratedSolidRuntimeHealthService.AddProviderSafely(...)` currently executes `if (issue != null) target.Add(issue);`. A malformed native runtime-health provider can therefore return a null `ModelHealthIssue` entry and have it silently discarded, producing a false-clean aggregate result. Core `ComprehensiveModelHealthService` already rejects null provider issues inside its isolation boundary so malformed provider output becomes a provider-failed diagnostic. V25 runtime health should preserve the same fail-visible contract while retaining its native `RUNTIME_HEALTH_PROVIDER_FAILED` code.

## Reserved scope

- Reject null `ModelHealthIssue` entries returned by native runtime-health providers instead of silently skipping them.
- Keep the rejection inside `AddProviderSafely(...)` so `IsRecoverableDiagnosticFailure(...)` converts it to `RUNTIME_HEALTH_PROVIDER_FAILED` with provider identity.
- Preserve all existing generated-solid ownership, sibling provider ordering and fatal-exception behavior.
- Add one focused static regression preflight for the V25 null-provider-issue contract.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Cad/GeneratedSolidRuntimeHealthService.cs`
- `scripts/preflight-runtime-health-null-provider-issue.py`
- this claim file

## Excluded scope

- No changes to Core comprehensive health; its equivalent contract is already implemented.
- No provider implementation rewrites.
- No mutation/write-path changes.
- No GitHub Actions, release publication, force push, or licensed BricsCAD V25/V26 runtime PASS claim.

## Validation plan

- Re-fetch current V25 service after claim registration before editing.
- Replace the null-issue silent skip with a deterministic exception inside the existing provider-isolation `try` block.
- Add a source preflight that rejects the old conditional-add pattern and requires null rejection plus `RUNTIME_HEALTH_PROVIDER_FAILED` isolation.
- Re-fetch source/preflight from current `main`, verify ancestry, and close with exact SHAs.

## Completion condition

Completed only when current `main` makes null native provider issues fail visible through `RUNTIME_HEALTH_PROVIDER_FAILED`, provider isolation/fatal exception behavior remains intact, focused regression source pins the contract, and this claim is `COMPLETED` with exact integration evidence.
