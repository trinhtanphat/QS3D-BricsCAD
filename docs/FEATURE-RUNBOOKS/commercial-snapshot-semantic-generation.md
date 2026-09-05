# Commercial snapshot semantic generation

## Scope

`CommercialAuditRecord.SourceRevisions` is an immutable detached provenance snapshot. For a source exposing deterministic collection Count evidence, cardinality stability alone is insufficient: the accepted counted generation must also be semantically stable before immutable publication.

## Contract

- Admit generic/read-only/non-generic Count evidence through the existing `CommercialGuard.Snapshot` Count contract.
- Preserve all existing negative, conflicting, maximum, transient-drift, overrun/no-overread, under-yield and post-traversal Count checks.
- After the first counted traversal and its final Count rebound, replay the source once before returning the immutable snapshot.
- During replay, continue rebinding the admitted Count around caller-controlled traversal boundaries.
- Compare `CommercialRevisionRef` by exact ordinal semantic state: `SourceKind`, `SourceId`, `RevisionId`.
- Reject any null, extra, missing, reordered or semantically changed replay item before publication.
- Pure streaming inputs without known Count remain one-pass and are not replayed.
- Preserve original snapshot ordering and detached immutable output.

## Deterministic regression

`CommercialSnapshotSemanticGenerationSmoke` covers:

1. stable Count with a different second semantic generation -> fail closed;
2. stable Count with freshly allocated but semantically equivalent revision objects -> success, proving value semantics rather than reference equality;
3. pure streaming source -> exactly one enumeration and successful immutable snapshot.

The focused auto-discovered source guard is `scripts/preflight-commercial-snapshot-semantic-generation.py`.

## Runtime classification

`REMOTE_SAFE / NOT_APPLICABLE` for licensed BricsCAD runtime. This is managed Core commercial/provenance correctness and is validated by deterministic smoke/static CI; no `LOCAL_PASS` is claimed.
