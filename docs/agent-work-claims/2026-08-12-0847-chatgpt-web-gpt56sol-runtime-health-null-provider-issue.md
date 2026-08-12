# Work claim — runtime health null provider issue

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-runtime-health-null-provider-issue-20260812-0847`
- Registered: `2026-08-12T08:47:00+07:00`
- Baseline main SHA: `98469144f23aa55c3a3b715316247138ea73fad2`
- Priority: owner-requested continue-all native runtime-health false-clean hardening

## Confirmed defect

`GeneratedSolidRuntimeHealthService.AddProviderSafely(...)` executed `if (issue != null) target.Add(issue);`. A malformed native runtime-health provider could therefore return a null `ModelHealthIssue` entry and have it silently discarded, producing a false-clean aggregate result. Core `ComprehensiveModelHealthService` already rejects null provider issues inside its isolation boundary so malformed provider output becomes a provider-failed diagnostic. V25 runtime health now preserves the same fail-visible contract while retaining its native `RUNTIME_HEALTH_PROVIDER_FAILED` code.

## Reserved scope

- Reject null `ModelHealthIssue` entries returned by native runtime-health providers instead of silently skipping them.
- Keep the rejection inside `AddProviderSafely(...)` so `IsRecoverableDiagnosticFailure(...)` converts it to `RUNTIME_HEALTH_PROVIDER_FAILED` with provider identity.
- Preserve all existing generated-solid ownership, sibling provider ordering and fatal-exception behavior.
- Add one focused static regression preflight for the V25 null-provider-issue contract.

## Implemented surfaces

- `src/QS3D.BricsCAD.V25/Cad/GeneratedSolidRuntimeHealthService.cs`
- `scripts/preflight-runtime-health-null-provider-issue.py`
- this claim file

## Integration evidence

- Claim registration: `4e7b9c19fdead17639cda29e0d50350000525f64`
- Source fix: `7becb586d1a33634f3879587a1973317de6bf288`
- Focused regression preflight: `5929c5f20b3a938a22f0448c7c1e583afb0ef64f`

## Validation performed

- Re-fetched the exact V25 service after claim registration; the null-provider conditional-add pattern was still present before editing.
- `AddProviderSafely(...)` now throws a deterministic `InvalidOperationException` when a provider yields a null issue, inside the existing `try` block, then the existing recoverable catch emits `RUNTIME_HEALTH_PROVIDER_FAILED` with `providerName` in the message.
- Re-fetched final source from current `main`; blob `6cce74534422974198030b8972ad81464125b27a` retains provider ordering, generated-solid ownership hardening, fatal exception exclusions and the new null-provider fail-visible contract.
- Re-fetched `scripts/preflight-runtime-health-null-provider-issue.py`; blob `4c547ef27c2516f96a4d5f5dbd17888d4a10a84c` rejects the old silent conditional-add and requires null rejection, recoverable isolation, `RUNTIME_HEALTH_PROVIDER_FAILED` and provider identity.
- The equivalent Core contract is independently established by `b98b977da70bd630d7df3b95b5b88b6cbba052ce`; this lane changes only V25 native runtime aggregation.

## Validation boundary

Remote source/static readback only. This session did not execute the repository preflight process, a full .NET build/test, GitHub Actions, or licensed BricsCAD V25/V26 runtime. No native runtime, private-DWG, installer, signing or release PASS is claimed.

## Excluded scope

- No changes to Core comprehensive health.
- No provider implementation rewrites.
- No mutation/write-path changes.
- No GitHub Actions, release publication or force push.

## Completion condition

Satisfied on the source/static contract: current `main` makes null native provider issues fail visible through `RUNTIME_HEALTH_PROVIDER_FAILED`, provider isolation/fatal exception behavior remains intact, focused regression source pins the contract, and exact integration evidence is recorded above.
