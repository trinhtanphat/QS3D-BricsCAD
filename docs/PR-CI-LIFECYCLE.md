# PR and CI lifecycle

This document clarifies branch CI, pull-request CI and protected merge checks.

## Core rule

Branch CI provides early exact-head evidence. The canonical PR is the review/merge carrier. Protected current-candidate checks are the hard merge gate.

The order of timestamps between branch-CI completion and PR creation is not a permanent carrier requirement.

## Normal flow

```text
commit + push canonical branch
  -> automatic branch CI starts
  -> open/update the canonical PR when ready for protected review
  -> fix any known red current-head evidence on the same branch
  -> protected PR `preflight` + `core`
  -> strict freshness + mergeability + collision checks
  -> merge when current, green and authorized
```

## Keep the canonical carrier

Do not replace a correct branch/PR solely because:

- a branch run completes after PR creation;
- CI is queued/running;
- the branch is behind `main` but can be reconciled safely;
- the carrier is red but the failure can be fixed in the same lane.

Do not reuse an older green result for a changed candidate.

## Candidate changes

Whenever the actual branch/merge candidate changes, obtain fresh applicable evidence.

If `main` moves and GitHub strict freshness requires reconciliation, update the same canonical branch safely and let the new candidate validate.

## Merge gate

Before merge:

- `preflight` is current and `SUCCESS`;
- `core` is current and `SUCCESS`;
- strict freshness is satisfied;
- the PR is mergeable;
- ownership/collision checks are clean;
- the current head/candidate is the intended task result;
- merge authorization under `MAIN-WRITE-AUTHORIZATION.md` applies.

`CI_POLICY.md` is authoritative for CI semantics. `MAIN-WRITE-AUTHORIZATION.md` is authoritative for merge permission.