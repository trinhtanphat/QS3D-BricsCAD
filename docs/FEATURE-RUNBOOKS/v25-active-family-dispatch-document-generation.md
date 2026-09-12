# V25 Active Family Direct Draw document-generation affinity

## Scope

This runbook covers `ActiveFamilyQuickDrawCommands` for `QS3DDRAWACTIVE`, `QS3DDRAWACTIVEADV`, and `QS3DDRAWACTIVEREPEAT`. The dispatcher is routing/orchestration only. Target Direct Draw commands remain responsible for prompts, native/semantic mutation, transactions, cancellation, and rollback.

## Defect and invariant

A managed BricsCAD `Document` can remain reference-equal while its native `Database` generation is replaced or reloaded. Managed-document equality plus QS3D project/family identity is therefore insufficient authority for a later authoring dispatch.

The dispatcher must:

- capture a non-zero `Document.Database.UnmanagedObject` identity together with the initial managed document/project/family snapshot;
- fail closed if that managed document is no longer `MdiActiveDocument` or its native database identity changes;
- revalidate exact document generation while refreshing the Active Family/project snapshot and again immediately before Quick/Advanced/repeated target handoff;
- preserve project `ProjectId`/`ChangeVersion`, Active Family identity/category, and routing checks;
- bind editor/palette publication to the same managed/native generation and revalidate after editor output before publishing palette state;
- keep target command transaction, mutation, cancellation, and rollback ownership unchanged;
- redact command failures to the existing generic user-facing message.

## REMOTE_SAFE verification

The following evidence can be established without a licensed BricsCAD GUI runtime:

1. Run `python scripts/preflight-v25-active-family-dispatch-document-generation.py`.
2. Run aggregate feature/source preflights according to repository CI policy.
3. Run deterministic smoke checks required by the shared workflow.
4. Build `QS3D.BricsCAD.V25` only through the repository's admitted/trusted locked BricsCAD V25 reference flow.
5. Require fresh exact-head CI after reconciling the latest protected `main`.

A REMOTE_SAFE green compile proves source/reference compatibility only. It does not prove native host behavior.

## LOCAL_ONLY licensed BricsCAD checks

These remain `LOCAL_ONLY / NO_RESULT` unless actually executed in a licensed BricsCAD V25 session:

- switch MDI document A -> B -> A while Active Family dispatch is being prepared;
- replace/reload the native database while retaining the same managed `Document` wrapper and confirm dispatch fails closed;
- exercise Quick and Advanced target commands across supported categories after project/family changes;
- exercise repeated Wall/Beam routing while document activation/deactivation and command prompting pump the host;
- close/destroy a document during dispatch preparation and confirm no stale editor/palette publication occurs;
- verify visible Workspace/palette state remains associated with the authoritative document generation.

Do not record `LOCAL_PASS` from source guards, CI, deterministic tests, or V25 compilation alone.
