# Aggregate preflight open-identity qualification

## Scope

This carrier hardens the aggregate feature-preflight runner against a pathname-to-open TOCTOU false negative when the runtime does not expose usable `st_dev`/`st_ino` identity.

## Contract

`_same_opened_file` keeps matching non-zero `(st_dev, st_ino)` authoritative. When those identifiers are unavailable, admission succeeds only when both snapshots expose equal `st_size`, `st_mtime_ns`, and `st_ctime_ns`. Missing fallback metadata fails closed rather than treating `None == None` as evidence that the opened file is the same generation.

The existing regular-file, repository-containment, source-size, `O_NOFOLLOW`, bounded-read, deterministic-child-environment, timeout, and process-tree cleanup protections remain unchanged.

## Deterministic verification

Run:

```text
python scripts/preflight-all-open-identity.py
python scripts/preflight-all.py
```

The focused guard covers matching and mismatching usable dev/inode identities, size drift, mtime drift, ctime drift, one-sided missing metadata, and metadata missing on both snapshots. The aggregate run must auto-discover the focused guard and complete without silently skipping it.

## Hosted admission

Before merge, require a fresh pull-request CI run bound to the final exact head SHA. Do not reuse GREEN from a pre-reconciliation head. If protected `main` advances, reconcile non-force, re-check the changed-path set, and run fresh exact-head CI again.
