# V26 held-candidate publisher execution

## Scope

This runbook covers the manual BricsCAD V26 release job boundary after an owner-confirmed qualification run has produced and uploaded one held candidate. It does not authorize a release dispatch, Authenticode signing, tag creation, asset upload, or licensed BricsCAD runtime execution by ordinary source agents.

## Required publication topology

1. The `qualify` job remains the only producer of the held V26 candidate and must complete before the `release` job.
2. The `release` job checks out the exact workflow SHA and downloads the exact tag/SHA-named qualification artifact.
3. Signed and unsigned candidate branches validate their expected asset set and call `assert-v26-candidate-identity.ps1` with `publish-v26-release.ps1` as the admitted publication script.
4. A null/failed candidate identity is terminal and must fail closed before publication.
5. Only after successful candidate admission does the release job invoke `publish-v26-release.ps1` exactly once. The publisher retains ownership of its transaction-safe lightweight-tag, draft-release, held-asset verification/upload, publish, acknowledgement-reconciliation, and rollback semantics.

The workflow must never invoke the publisher before candidate admission or independently of the admitted script identity.

## Validation

Source-safe validation is:

```text
python scripts/preflight-v26-held-candidate-publisher-execution.py
python scripts/preflight-all.py
```

Normal protected PR validation must then provide fresh exact-candidate `preflight` and `core` success with strict freshness before merge.

Do not use a source/CI run to claim a V26 release was actually published, signed, or qualified in licensed BricsCAD. Actual stable release execution remains separately owner-controlled and requires the release workflow's confirmation/runtime/signing prerequisites.
