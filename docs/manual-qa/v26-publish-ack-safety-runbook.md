# V26 publish acknowledgement safety qualification

## Scope
This qualification covers transport ambiguity around the final GitHub Release `draft=false` PATCH in `scripts/publish-v26-release.ps1`.

## Required invariants
1. Publication safety becomes unproven before the final PATCH is attempted.
2. An acknowledged PATCH must be followed by protected-main revalidation before publication safety can be cleared and returned release identity can be accepted.
3. If PATCH is committed server-side but its response is lost, reconciliation of an observed published release must independently revalidate protected main before safety can be cleared.
4. A known post-PATCH safety failure remains fail-closed; acknowledgement recovery must not convert it to success.
5. Release identity, exact workflow SHA, tag, asset identities, checksums/signatures and prerelease state remain independently verified.
6. Once publication may already be public, the workflow must not perform destructive rollback merely to conceal acknowledgement ambiguity.

## Scenarios
- Stable main + acknowledged PATCH: post-PATCH validation succeeds, safety clears, exact release identity is accepted.
- Stable main + committed PATCH + lost acknowledgement: reconciliation observes the published transaction, independently revalidates main, then accepts only the exact qualified release.
- Main drifts before or during PATCH: publication remains fail-closed/manual-review.
- Post-PATCH validation observes drift: the known safety failure cannot be cleared by later reconciliation.
- Reconciliation main query fails: safety remains unproven and the run fails; no ambiguous success is reported.

## CI admission
`scripts/preflight-v26-publish-ack-safety.py` must be auto-discovered and pass on the exact candidate SHA together with all required repository preflight/core checks. Any reconcile onto newer protected `main` requires a fresh exact-head run.
