# ProjectPersistenceStamp nested-state stability

`ProjectPersistenceStamp` decides whether the in-memory QS3D project still matches the last saved persistence state. Its capture boundary must therefore represent one stable view of persisted content, including nested Family/Element state that can change without incrementing `ProjectState.ChangeVersion`.

## Contract

`CaptureStableSnapshot` performs two bounded complete materializations of tracked metadata and nested persisted content under the same project-level persistence boundary. The capture is admitted only when:

- the project-level boundary remains unchanged;
- tracked metadata is identical across both passes;
- serialized nested persisted content is identical across both passes.

Any mismatch fails closed with `InvalidOperationException`. The implementation does not retry indefinitely or silently accept a mixed-time snapshot.

Existing semantics remain unchanged: Workspace presentation metadata is excluded from semantic dirty tracking, recovered-backup state requires save, collection/count bounds remain fail-closed, and `RequiresSave`/`MarkSaved` continue to bind to the same project instance.

## Deterministic regression

`ProjectPersistenceStampNestedStabilitySmoke` installs a test-only mutating `IDictionary` behind one Family's property store. On the first property enumeration it changes the Family name after that name has already been serialized. This mutation deliberately does not bump the parent project revision. A single-pass implementation accepts the mixed-time result; the two-pass admission must reject it. A control also proves that a normal nested Family mutation after a stable capture makes `RequiresSave` return true.

Validation surfaces:

- registered Core smoke: `ProjectPersistenceStampNestedStabilitySmoke`;
- auto-discovered source guard: `scripts/preflight-project-persistence-stamp-nested-stability.py`;
- normal protected Shared CI `preflight + core`.
