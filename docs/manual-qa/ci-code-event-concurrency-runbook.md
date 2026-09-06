# Shared CI code-event concurrency

## Incident

Shared CI intentionally validates both `push` events on `agent/**` / `integration/**` branches and `pull_request` events. Before #5998, the concurrency key used `github.ref` for push events but PR number for pull-request events. An open PR branch update therefore launched two independent full CI executions for one exact commit.

Exact-SHA evidence: `1bfa895bd178d49ad5ea5e16728d9cfebdcbfcf8` produced push run `34052691948` with successful preflight/core and pull-request run `34052693551` with a failed aggregate source-guard step. The repository must not create contradictory exact-head evidence merely because the same code event is represented twice.

## Contract

Code-validation concurrency identity is `(workflow, head repository, head branch, code-class)`:

- same-repository push and pull-request synchronize events for the same branch resolve to the same cancellation group;
- fork PRs include their head repository identity so equal branch names in unrelated forks cannot cancel each other;
- metadata-only PR `edited` events remain in a separate `metadata` class and cannot cancel code validation;
- `cancel-in-progress` remains enabled;
- exact candidate SHA binding, reservation admission, source guards, smoke tests, and build gates are unchanged.

The auto-discovered `scripts/preflight-ci-code-event-concurrency.py` fails closed if the workflow regresses to PR-number-versus-ref concurrency or drops repository/branch isolation.

## Verification

For an open same-repository agent branch, push a new commit. GitHub may emit both push and pull-request synchronize events, but they must resolve to one code cancellation domain. The later canonical run may cancel the earlier duplicate; it must still validate the exact branch head. A metadata-only PR edit uses a distinct cancellation class and must not cancel in-flight code validation.
