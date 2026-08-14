# Work claim — QSC profile canonical identity

- Status: `ACTIVE`
- Agent: `gpt56sol-qsc-profile-canonical-identity-20260814-0810`
- Registered: `2026-08-14T08:10:00+07:00`
- Baseline main SHA: `a82b3c993579d00643bfdad862a4cd6d6610a582`
- Priority: `P2` QSC-01 declarative-rule identity integrity.

## Verified gap

`QsRuleDefinition.RequireIdentity()` currently calls `Trim()` and returns the trimmed token. As a result, distinct supplied representations such as `" QSC.A "` and `"QSC.A"`, padded health issue codes, and padded profile IDs are silently collapsed into one declarative identity. QSC rule/profile IDs are stable configuration identities used for deterministic ordering, duplicate detection and health-code resolution; silently rewriting their representation weakens canonical identity guarantees and can hide malformed persisted/configured input.

The existing `QsRuleProfileSmoke` rejects blank identities and internal whitespace/unsupported characters but does not reject surrounding whitespace. Current source/history has no focused canonical-representation hardening for this contract.

## Reserved scope

- `src/QS3D.Core/Diagnostics/QsRuleProfile.cs`
- `tests/QS3D.Core.SmokeTests/QsRuleProfileSmoke.cs`
- this claim file.

## Intended bounded change

- Require rule IDs, health issue codes, and profile IDs to already equal their trimmed representation; surrounding whitespace fails closed instead of being normalized.
- Preserve the existing allowed identity character set, case-insensitive duplicate/resolution semantics, deterministic rule ordering, and detached profile behavior.
- Preserve explanation-text normalization semantics; explanations are descriptive text, not stable identity tokens.
- Add focused regression for padded rule ID, padded health-code ID and padded profile ID while preserving existing positive/negative smoke cases.

## Excluded scope

- ModelHealth predicates or `ModelHealthIssue` runtime emission semantics.
- QSC rule-family coverage/logic, severity policy changes, UI/report rendering, persistence format changes, BricsCAD/native behavior.
- V25 release automation, IFC, Rebar and other current claims.

## Validation plan

- Refresh current `main` and exact-path claims after this claim-only commit.
- Re-read the two reserved files and recheck recent commits for overlap before writing source/tests.
- Publish source + focused regression to current `main` without force.
- Re-fetch files and verify ancestry/current-main inclusion.
- Run local managed build/smoke only if this environment has the required .NET toolchain; otherwise record the gate as unexecuted. Do not dispatch GitHub Actions and do not claim native PASS.

## Completion condition

Canonical identity input is enforced without changing QSC business predicates; focused regression is on current `main`; remote readback/ancestry is verified; actual validation evidence and remaining gates are recorded; claim is closed `COMPLETED`.