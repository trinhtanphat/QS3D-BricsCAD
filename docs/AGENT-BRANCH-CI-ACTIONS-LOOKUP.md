# Agent branch / PR CI lookup

Use this document when an agent needs to resolve CI evidence for its own canonical carrier.

## Purpose

The shared validation workflow is `.github/workflows/ci.yml`.

A branch push validates an exact branch SHA. A pull request run validates GitHub's current PR candidate against its target. These are different evidence classes.

## Branch lookup

For the current canonical branch:

1. resolve its exact head SHA;
2. find the automatic `ci.yml` push run associated with that branch/head when observable;
3. bind any PASS/FAIL claim to that exact SHA;
4. if red, inspect the failing job/step/log and remediate the same branch;
5. do not reuse a run for an older head.

Branch CI is early feedback. It is not permanent PR identity.

## PR lookup

For the canonical PR:

1. resolve the actual current head/candidate shown by GitHub;
2. inspect required `preflight` and `core` contexts;
3. require current candidate SUCCESS before merge;
4. respect strict freshness and mergeability;
5. do not close/recreate a correct PR merely because a branch run completed after PR creation.

## Pending evidence

If an intermediate progress update is useful, report the exact available:

- workflow/run identifier;
- tested SHA/candidate;
- current job/step;
- already satisfied gates;
- remaining gates.

Do not invent unavailable identifiers.

## Red evidence

A known red branch or protected check on the current owned carrier is an automatic diagnosis/fix/push/recheck trigger while safe same-lane remediation exists.

## Merge gate

Branch CI alone never authorizes merge. The final gate is the protected current PR candidate plus the authorization in `docs/MAIN-WRITE-AUTHORIZATION.md`.

See `CI_POLICY.md` and `docs/PR-CI-LIFECYCLE.md` for semantics.