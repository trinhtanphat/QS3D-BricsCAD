# Work claim — AuditTrail action canonicality

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:18:00+07:00`
- Baseline main SHA: `2dee205ec1e1ebe8cdbbdf8e703b9c61dd78699f`
- Priority: evidence-driven Core audit integrity

## Confirmed defect

`AuditTrail.Record` currently accepts `null`, blank, whitespace-only, and padded action names. It stores `null` as an empty string and stores padded actions verbatim. For project-bound trails, the method calls `ProjectState.Touch()` and appends the event without first validating the semantic action identity. A malformed action can therefore advance `ChangeVersion` and enter non-canonical audit history.

## Reserved scope

Require every recorded audit action to be non-blank and canonicalize valid action text by trimming outer whitespace before any project mutation. Preserve the existing payload semantics for element id, detail, actor and correlation id.

## Expected surfaces

- `src/QS3D.Core/Audit/AuditTrail.cs`
- `tests/QS3D.Core.SmokeTests/AuditTrailActionCanonicalitySmoke.cs`
- `tests/QS3D.Core.SmokeTests/AuditTrailActionCanonicalityRegistration.cs`
- this claim file

## Excluded scope

- No action vocabulary/enum migration.
- No new maximum-length or control-character policy in this lane.
- No changes to element/detail/actor/correlation normalization.
- No caller/native BricsCAD command changes.
- No persistence schema change.
- No GitHub Actions dispatch.

## Validation plan

- Reject `null`, empty and whitespace-only action values before `ProjectState.Touch()` or event append.
- Canonicalize a padded valid action to its trimmed value.
- Confirm rejected calls leave project `ChangeVersion`, `UpdatedUtc`, and authoritative audit event count unchanged.
- Confirm a valid call still increments `ChangeVersion` exactly once and preserves element/detail/actor/correlation payloads.
- Preserve existing snapshot and max-version atomicity behavior.
- Use a dedicated module initializer registration file, re-fetch the target blob immediately before product write, inspect exact pushed diffs and verify ancestry.
- No .NET/V25/V26 runtime PASS will be claimed unless actually executed.

## Coordination

Recent commit search for `AuditTrail` found audit-owned caller revisions but no change to `AuditTrail.cs`. The existing `AuditTrailSnapshotSmoke` covers snapshot isolation and max-version atomicity, not action canonicality. No exact `AuditTrail.cs` reservation was found in the current claim listing/search.

## Completion condition

Invalid audit actions fail before mutation, valid actions are stored canonically, focused regression source is on current `main`, concurrent work is preserved, and this claim is closed with exact commit SHAs.