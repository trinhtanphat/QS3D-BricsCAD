# Work claim — QSC profile canonical identity

- Status: `COMPLETED`
- Agent: `gpt56sol-qsc-profile-canonical-identity-20260814-0810`
- Registered: `2026-08-14T08:10:00+07:00`
- Completed: `2026-08-14T08:13:00+07:00`
- Baseline main SHA: `a82b3c993579d00643bfdad862a4cd6d6610a582`
- Priority: `P2` QSC-01 declarative-rule identity integrity.

## Verified gap

`QsRuleDefinition.RequireIdentity()` silently trimmed supplied rule/profile/health-code identities. Distinct input representations could therefore collapse into one declarative identity before deterministic ordering, duplicate detection and health-code resolution.

## Reserved scope

- `src/QS3D.Core/Diagnostics/QsRuleProfile.cs`
- `tests/QS3D.Core.SmokeTests/QsRuleProfileSmoke.cs`
- this claim file.

## Implementation

- Claim-only commit: `7992405a7c23e0110b8dc6c6cf29dd1485b48e9b`.
- Source commit: `9a02a90762ef88ac3541310d28d726a24216788f`.
  - `RequireIdentity()` now requires the supplied identity to already equal its trimmed representation.
  - Surrounding whitespace fails closed instead of being normalized.
  - Existing allowed-character validation, case-insensitive duplicate/resolution semantics and deterministic ordering remain unchanged.
  - Explanation text retains its existing trim semantics because it is descriptive text rather than a stable identity token.
- Regression commit: `4de61a97362003072e0918b7a35e12f53671f2e6`.
  - padded `ProfileId` rejected;
  - padded `RuleId` rejected;
  - padded `HealthIssueCode` rejected;
  - existing deterministic profile, case-insensitive resolution, ambiguity, malformed-character and explanation regressions remain intact.

## Validation actually performed

- Refreshed `main` immediately before claim and after claim-only publication; concurrent changes did not touch the reserved QSC files.
- Re-read both reserved source/test files from commit `4de61a97362003072e0918b7a35e12f53671f2e6`; remote readback confirms the canonical guard and all three focused regressions are present.
- Local managed build/smoke was **not executed**: this execution environment exposes none of `dotnet`, `csc`, `mcs`, `msbuild`, `xbuild`, or `mono`. No managed PASS is claimed.
- No GitHub Actions were dispatched.
- No BricsCAD native runtime test was executed or claimed.

## Excluded scope preserved

No ModelHealth predicate/runtime issue emission, QSC rule-family business logic, severity policy, UI/report, persistence, V25 release automation, IFC, Rebar, or native behavior was changed.

## Remaining gates

A full Core build/deterministic smoke run in an environment with the repository checkout and .NET toolchain remains appropriate before using this change as fresh executable release evidence.

## Completion condition

Satisfied for repository implementation/coverage: canonical QSC identity input is enforced, focused regressions are committed and read back from `main`, no unrelated QSC business predicate was changed, validation limitations are recorded without fabricated PASS claims, and the claim is `COMPLETED`.