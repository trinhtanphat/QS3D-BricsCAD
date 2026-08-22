# Work claim — AuditTrail action control-character canonicality

- Status: `COMPLETED`
- Agent: `ChatGPT / GPT-5.6 Sol`
- Registered: `2026-08-12`
- Baseline main SHA: `764a6ee6078af5267e19be376ebe5d9acf936a76`
- Priority: owner-requested whole-repository audit; Core audit persistence integrity

## Verified defect

`AuditTrail.Record(...)` trimmed and rejected blank actions, but an otherwise nonblank action could still contain control characters such as `\u0001`. The standard audit API therefore admitted an action token that was not a safe canonical persisted identifier; QSDB later serializes `AuditEvent.Action` into an XML attribute, so XML-forbidden control characters could turn an accepted semantic mutation into a later save failure.

## Delivered

- Claim registration: `8ef4a317358fc4b6fbbab2f210ca54babb08883c`
- Source fix: `c5561c69efd76551dbadad4b65a42099a74615da`
- Focused regression: `e9857bd81689a5fe132478a4bd7efd6cbb98dfa2`
- `AuditTrail.Record(...)` now rejects any control character in the normalized action before project revision/history mutation.
- Existing-history preflight now treats control-character action tokens as non-canonical as well.
- Existing valid action trimming and payload semantics remain unchanged.

## Reserved scope

- `src/QS3D.Core/Audit/AuditTrail.cs`
- `tests/QS3D.Core.SmokeTests/AuditTrailActionCanonicalitySmoke.cs`
- this claim file

## Verification

- Re-fetched committed source blob on main: `ca8d190c494489f51f6493c4711b1551bf867873`.
- Re-fetched committed smoke blob on main: `f177606f12ea32e7bbf6c1b9b2a3a7ff233a5798`.
- Compared source commit `c5561c69...` to observed main/test commit `e9857bd8...`: `behind_by: 0`; the source commit is an ancestor of the regression commit.
- Regression asserts `\u0001` action rejection leaves `ChangeVersion`, `UpdatedUtc`, and audit-event count unchanged.
- The smoke was committed but was not executed in this connector-only environment; GitHub Actions were not dispatched and no CI/runtime PASS is claimed.
- No force-push, no release publication, and no BricsCAD V25 runtime PASS claim.
