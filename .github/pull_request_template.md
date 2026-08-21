# Pull request

## Scope

Leaf Issue: <!-- REQUIRED for new ordinary agent tasks: the concrete GitHub Issue #N; never a guessed number or umbrella/control Issue -->
Parent/Umbrella: none <!-- relationship only; never reuse this number as the child Lane-Key -->
Lane-Key: <!-- REQUIRED: issue-N where N is the actual Leaf Issue number for new ordinary issue-backed work -->
Canonical owner/session: <!-- stable account/session/automation-specific identity; generic chatgpt/gpt56sol/C0/W1 alone is not unique -->
Canonical carrier: <!-- exactly one active task branch; new ordinary form: agent/<owner-token>/issue-N-<short-scope> -->
Supersedes: none <!-- if not none, name the explicitly superseded closed PR/branch -->
Task branch: <!-- same exact canonical carrier as above -->
Baseline `main` SHA: <!-- exact 40-hex SHA used to start/reconcile this work -->
Head SHA: <!-- exact candidate SHA validated by branch CI -->

Summarize the behavior changed and the user-visible reason for the change. Keep unrelated work out of this PR.

For concurrent-agent work, `docs/AGENT-IDENTITY-AND-BRANCH-NAMESPACE.md` and `docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md` are mandatory. A Git branch has a ref name, not a GitHub `#number`. Never guess/reuse a future Issue/PR number. One Lane-Key has one active owner and one canonical carrier. A stale/red/behind carrier is not free for takeover; explicit supersession must be recorded and the old PR closed before a replacement is represented as canonical.

## Identity / collision validation

- [ ] The Leaf Issue is a concrete task Issue, not merely a parent/umbrella/control Issue.
- [ ] For new ordinary issue-backed work, `Lane-Key` is exactly `issue-N` using the actual GitHub-returned Leaf Issue number.
- [ ] No Issue or PR number was guessed, predicted from the latest number, or reserved before GitHub allocated it.
- [ ] After creating/reusing the Leaf Issue, I repeated the semantic ownership collision check before branch mutation/push.
- [ ] The canonical branch contains the actual Leaf Issue number and an owner/session namespace; generic AI/schedule labels alone are not treated as globally unique ownership.
- [ ] If an equivalent earlier valid reservation became visible during stabilization, this carrier stopped instead of racing it.

## Validation

- [ ] I checked relevant Issues/PRs/active lanes and did not overwrite concurrent work.
- [ ] This PR is the **only open canonical carrier** for its Lane-Key; if it supersedes an older carrier, that supersession was explicitly recorded and the old PR is closed.
- [ ] I did not create this PR merely to reconcile/transport `main` or another task branch into an agent branch.
- [ ] Watched-path work has the exact branch-CI evidence required by the current canonical CI lifecycle policy.
- [ ] I refreshed `main` after branch validation and reconciled the candidate if `main` moved when required.
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

`origin/main` remains direct-write read-only. A green PR does **not** authorize its own merge. Normal owner-requested task PRs follow `docs/MAIN-WRITE-AUTHORIZATION.md`: merge only through the protected PR path when the same-task standing authorization applies and every required current gate is satisfied. Never interpret a green PR as permission to merge unrelated work or bypass protection.
