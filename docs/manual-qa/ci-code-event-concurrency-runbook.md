# Shared CI code-event concurrency

## Incident history

Shared CI intentionally validates both `push` events on `agent/**` / `integration/**` branches and `pull_request` events. Before #5998, push and PR events used unrelated concurrency identities and could run duplicate full validation for one exact commit. #5998 coalesced same-branch push and PR code events into one cancellation group.

Hosted evidence after #5998 exposed a more serious protected-branch interaction: GitHub persists the losing event's jobs as cancelled check-runs on the candidate SHA. PR #5962 exact SHA `59841875ffc6e32e17d1c987ce612768529f787e` had successful PR `preflight`/`core` from run `34059071502`, but cancelled duplicate `preflight`/`core` from push run `34059070007`; the repository ruleset rejected merge because both required contexts also had cancelled instances.

#6004 therefore supersedes the cross-event coalescing part of #5998. Avoiding duplicate compute is secondary to preserving unambiguous required-check admission.

## Contract

Concurrency identity is `(workflow, head repository, head branch, event class)`:

- push, pull-request code, pull-request metadata edits, and manual dispatch use distinct cancellation classes;
- superseded runs still cancel within the same event class;
- fork PRs include their head repository identity so equal branch names in unrelated forks cannot cancel each other;
- pull-request code validation alone owns protected required names `preflight` and `core`;
- branch push uses `branch-preflight` / `branch-core`;
- `pull_request(edited)` uses `metadata-preflight` / `metadata-core`;
- manual dispatch uses `dispatch-preflight` / `dispatch-core`;
- exact candidate SHA binding, reservation admission, source guards, smoke tests, build gates, and fail-closed semantics are unchanged.

The auto-discovered `scripts/preflight-ci-code-event-concurrency.py` rejects regression to PR-number/ref divergence, loss of branch/fork isolation, or loss of event-class separation. `scripts/preflight-ci-required-check-cancellation.py` separately pins the required-check ownership/name contract.

## Verification

For an open same-repository agent branch, push a new commit. GitHub may emit both push and pull-request synchronize events. They may run concurrently, but push must expose branch-prefixed contexts while PR code validation exposes the stable required `preflight` and `core`; neither event may cancel the other. A metadata-only PR edit must use metadata-prefixed contexts and must not cancel an in-flight code-validation run. A manually dispatched run must use dispatch-prefixed contexts and cannot satisfy the PR ruleset.

Before merge, inspect the exact candidate SHA: there must be a terminal GREEN PR-code `preflight` and `core`, and no cancelled instance of either required name from a non-PR-code event family.
