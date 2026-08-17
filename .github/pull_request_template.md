# Pull request

## Scope

Lane-Key: <!-- REQUIRED for agent/integration PRs: issue-123 or an explicit stable batch key -->
Canonical owner/session: <!-- stable agent/session/lane id -->
Canonical carrier: <!-- exactly one active task branch for this Lane-Key -->
Supersedes: none <!-- if not none, name the explicitly superseded closed PR/branch -->
Issue: <!-- #123; reuse the existing task issue instead of creating a duplicate -->
Task branch: <!-- agent/<agent-id>/<scope> or integration/<batch-id> when explicitly authorized -->
Baseline `main` SHA: <!-- exact 40-hex SHA used to start/reconcile this work -->
Head SHA: <!-- exact candidate SHA validated by branch CI -->

Summarize the behavior changed and the user-visible reason for the change. Keep unrelated work out of this PR.

For concurrent-agent work, `docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md` is mandatory. One Lane-Key has one active owner and one canonical carrier. A stale/red/behind carrier is not free for takeover; explicit supersession must be recorded and the old PR closed before a replacement is represented as canonical.

## Validation

- [ ] I checked relevant Issues/PRs/active lanes and did not overwrite concurrent work.
- [ ] This PR is the **only open canonical carrier** for its Lane-Key; if it supersedes an older carrier, that supersession was explicitly recorded and the old PR is closed.
- [ ] I did not create this PR merely to reconcile/transport `main` or another task branch into an agent branch.
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
