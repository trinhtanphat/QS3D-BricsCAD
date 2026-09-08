# V25 cloud packaged-sample dispatcher fence

## Scope

This runbook covers the REMOTE_SAFE source/CI contract that keeps the protected-main V25 cloud dispatcher aligned with tracked package inputs under `samples/generated/`.

It does **not** claim or require a release dispatch, publication, signing operation, licensed BricsCAD execution, private DWG evidence, or `LOCAL_PASS`.

## Root cause guarded

`scripts/package-v25.ps1` reads tracked synthetic fixtures from `samples/generated/` and copies those bytes into the V25 release package. The protected-main dispatcher therefore has two independent responsibilities for that root:

1. its `push.paths` trigger must start the release-decision lane after a sample-only protected-main landing; and
2. its in-job `release_relevant_pathspecs` classifier must treat newer sample changes as release-relevant while deciding whether an older debounced/running dispatcher has been superseded.

If either surface omits the package-input root, automatic release admission can miss or misclassify a newer package generation even when the downstream release workflow has its own final freshness fence.

## Source contract

`.github/workflows/dispatch-v25-cloud-after-main-integration.yml` must contain exactly one:

- `samples/generated/**` entry in the protected-main `push.paths` block; and
- `samples/generated/` entry in `release_relevant_pathspecs`.

The existing exact-source SHA validation, ancestry checks, fail-closed `git diff` handling, final protected-main API/fetch rebinding, active-release serialization, preview ordinal reservation and downstream dispatch safety remain unchanged.

## Deterministic regression

Run from repository root:

```text
python scripts/preflight-v25-cloud-packaged-sample-dispatch.py
```

Expected terminal output:

```text
PASS V25 cloud packaged-sample dispatcher fence
```

The guard binds the dispatcher to the package script's actual `samples/generated` source marker and mutation-probes omission from the trigger and supersession classifier independently. The script is named `preflight-*.py`, so the shared aggregate feature-guard discovery executes it automatically.

## Hosted evidence

For the canonical carrier, require fresh exact-head Shared CI. Protected-PR merge admission requires terminal `preflight` and `core` SUCCESS on the current candidate after any latest-main reconciliation. Historical GREEN from an older head is not reusable.

A hosted source/CI pass proves only the dispatcher and package-source contracts above. It does not prove a commercial V25 release was published or that licensed BricsCAD runtime qualification passed.
