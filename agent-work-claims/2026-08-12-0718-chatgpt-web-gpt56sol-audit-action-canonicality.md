# Work claim — AuditTrail action canonicality

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:18:00+07:00`
- Completed: `2026-08-12T07:28:00+07:00`
- Baseline main SHA: `2dee205ec1e1ebe8cdbbdf8e703b9c61dd78699f`
- Priority: evidence-driven Core audit integrity

## Confirmed defect

`AuditTrail.Record` accepted `null`, blank, whitespace-only, and padded action names. It stored `null` as an empty string and stored padded actions verbatim. For project-bound trails, the method called `ProjectState.Touch()` and appended the event without first validating the semantic action identity. A malformed action could therefore advance `ChangeVersion` and enter non-canonical audit history.

## Reserved scope

Require every recorded audit action to be non-blank and canonicalize valid action text by trimming outer whitespace before any project mutation. Preserve the existing payload semantics for element id, detail, actor and correlation id.

## Product commits

- Claim registration: `9179d12ba80a2db8cff978231b96116c6fb5982f`
- Core fix: `dc7917de6289862d284dc416b6934fcf8b0d0f0c` — `fix(audit): canonicalize recorded action names`
- Focused smoke: `425b6c31b08701579b276723a15af7ad9c246b96` — `test(audit): cover action canonicality`
- Smoke registration: `985b81235fd4cd4b3a350a4e0e5d865a9a0cf664` — `test(audit): register action canonicality smoke`

## Implemented contract

- `null`, empty and whitespace-only actions now throw `ArgumentException` before project mutation or event append.
- Valid action text is trimmed before storage.
- Element id, detail, actor and correlation id retain their existing payload semantics.
- Valid project-bound records continue to advance `ChangeVersion` exactly once.
- Existing max-version atomicity remains structurally intact because action validation occurs before the existing `Touch()`/append ordering and no `Clear()` behavior changed.

## Regression coverage

`AuditTrailActionCanonicalitySmoke` covers:

- null/empty/whitespace rejection;
- no rejected-call changes to `ChangeVersion`, `UpdatedUtc`, or authoritative event count;
- padded action canonicalization;
- exact payload preservation for element/detail/actor/correlation fields;
- exactly one revision increment and one append on a valid record.

A dedicated module initializer registration avoids modifying shared smoke registration surfaces.

## Validation actually performed

- Re-fetched the claim from `main` before product work.
- Re-fetched `AuditTrail.cs` immediately before update and wrote against blob `d7251ad2f22893abf0b0f4353d41fae9ff38d0c8`.
- Reviewed exact source diff for `dc7917de6289862d284dc416b6934fcf8b0d0f0c`; only `AuditTrail.Record` action validation/storage changed.
- Reviewed exact focused smoke diff for `425b6c31b08701579b276723a15af7ad9c246b96`.
- Verified `985b81235fd4cd4b3a350a4e0e5d865a9a0cf664` remains an ancestor of current `main`; subsequent concurrent commits were disjoint.
- GitHub Actions were not dispatched or rerun.
- No hosted .NET test runner or BricsCAD V25/V26 runtime PASS is claimed in this session.

## Completion condition

Satisfied: invalid audit actions fail before mutation, valid actions are stored canonically, focused regression source is on current `main`, concurrent work was preserved, and the claim is released.