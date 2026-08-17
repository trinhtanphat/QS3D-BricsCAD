# Pull request

## Scope

Issue: <!-- #123; reuse the existing task issue instead of creating a duplicate -->
Lane-Key: <!-- stable identity, normally issue-123; exactly one ACTIVE owner/canonical carrier may claim it -->
Lane owner/session: <!-- stable agent/session identity recorded on the reservation -->
Canonical carrier: <!-- this PR's agent/<agent-id>/<scope> branch; do not create a second carrier for the same Lane-Key -->
Supersedes: <!-- none, or explicit prior carrier/PR plus the recorded reassignment/supersession authority -->
Task branch: <!-- agent/<agent-id>/<scope> or integration/<batch-id> when explicitly authorized -->
Baseline `main` SHA: <!-- exact 40-hex SHA used to start/reconcile this work -->
Head SHA: <!-- exact candidate SHA validated by branch CI -->

Summarize the behavior changed and the user-visible reason for the change. Keep unrelated work out of this PR.

A stale, red, behind, blocked, or slow carrier is still the canonical carrier until its reservation is explicitly released, superseded, or reassigned. Do not create a competing implementation merely because another carrier can be made cleaner or greener.

## Validation

- [ ] I checked relevant Issues/PRs/active lanes and did not overwrite concurrent work.
- [ ] This PR declares one stable Lane-Key and is the canonical carrier recorded for that lane; any supersession is explicit and already recorded.
- [ ] I did not create an internal branch-to-branch PR solely to replay/sync `main`; reconciliation stayed on the canonical task carrier and was non-force.
- [ ] Watched-path work has a successful Shared Branch CI run on the exact current head SHA before this PR was opened/updated.
- [ ] I refreshed `main` after branch CI and reconciled the candidate if `main` moved.
- [ ] Required PR checks (`preflight` and `core`) are green on the current merge candidate, or are still running and this PR is not being represented as merge-ready.
- [ ] Any failure was fixed at its root cause; no assertion/check was weakened just to make CI green.

## Runtime / host evidence

BricsCAD target: <!-- V25 / V26 / Core-only / not applicable -->
Licensed runtime status: <!-- PASS with run/evidence, PENDING_LOCAL, or not applicable -->

- [ ] I did not claim licensed BricsCAD runtime PASS without actual licensed runtime evidence.
- [ ] If native/runtime validation is still required, the PR explicitly says `PENDING_LOCAL` and identifies what remains.

## Release impact

- [ ] No release impact.
- [ ] V25 cloud preview path is affected.
- [ ] V25 commercial release path is affected.
- [ ] V26 release path is affected.

For release-impacting work, describe exact source/tag/package/provenance implications and keep release publication fail-closed.

## Merge authorization

A green PR does **not** authorize its own merge. Normal agents stop before `main`; merge/integration requires explicit owner authorization under `docs/MAIN-WRITE-AUTHORIZATION.md`.
