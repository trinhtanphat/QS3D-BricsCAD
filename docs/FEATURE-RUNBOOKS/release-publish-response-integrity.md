# V25/V26 successful publish-response integrity

## Scope

This REMOTE_SAFE contract covers the final GitHub Release publish acknowledgement for the manual V25 and V26 release workflows. It does not dispatch a release, fabricate signing evidence, or claim licensed BricsCAD runtime acceptance.

## Threat boundary

A successful HTTP response from the final `PATCH .../releases/{id}` is still remote input. `draft=false` alone is not sufficient proof that GitHub committed the exact release transaction that QS3D qualified before publication.

Both release generations must validate the successful PATCH response through the same `Assert-PublishedReleaseMatchesVerifiedTransaction` authority used by ambiguous acknowledgement recovery. The proof binds the expected release identity/URI, exact tag and workflow SHA, prerelease state, verified asset identities and local byte lengths; V25 additionally binds its release name and transaction marker. V26 retains its V26-only asset isolation and existing release metadata contract.

## Fail-closed behavior

If a successful PATCH returns a mismatched or incomplete snapshot, the exact-transaction assertion throws into the existing publication catch path. That path performs an authoritative GET. An exact already-published transaction is accepted as committed; an exact draft remains eligible for the existing bounded rollback; ambiguous or mismatched authoritative state fails with manual cleanup required. The workflow must never accept a publication merely because the PATCH payload says `draft=false`.

## Deterministic validation

`scripts/preflight-release-publish-response-integrity.py` requires both workflows to call the exact published-transaction assertion immediately after obtaining the final PATCH response and before leaving the success path. It pins all version-specific arguments and contains mutation probes that remove the `$published` response binding independently for V25 and V26.

Repository-safe validation is source/preflight CI only. Production release dispatch, certificates/timestamps, and licensed V25/V26 runtime evidence remain outside this remote carrier.
