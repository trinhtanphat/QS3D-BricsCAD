# V25 Slab Mesh post-commit outcome

Issue: #7075
Lane: C03

## Defect
`SlabMeshSolidBuilder.BuildSelected` updates semantic state and commits the native CAD transaction before nested transaction/document-lock cleanup completes. A cleanup exception after `transaction.Commit()` was rethrown to `SlabMeshCommands`, which then reported generic mutation failure although CAD/project state was already durable. Retrying could duplicate an already-committed authoring operation. The command also refreshed palette/Regen/status/editor output without proving the captured managed document still owned the same native database generation.

## Contract
- Before native commit, failures restore the captured `ProjectStateSnapshot`; rollback failure remains terminal and preserves both errors internally.
- After native commit, transaction/document-lock cleanup failure is a committed outcome with a bounded warning, never mutation failure and never project rollback.
- The builder returns committed element/bar counts plus `PostCommitCleanupWarning`; raw cleanup exception detail is never published.
- Native mutation is admitted only for the exact active managed document and captured non-zero `Database.UnmanagedObject` generation.
- Palette refresh, Regen, status and editor publication are revalidated against that same document/native generation so stale UI cannot be published after MDI or same-wrapper database replacement.
- Slab mesh footprint, generated ownership, selection, project ChangeVersion/target freshness, geometry bounds and audit-before-commit semantics remain unchanged.

## REMOTE_SAFE qualification
Run:

```text
python scripts/preflight-v25-slabmesh-postcommit-outcome.py
python scripts/preflight-slab-mesh.py
python scripts/preflight-slab-mesh-native.py
python scripts/preflight-generated-rebar-atomicity.py
python scripts/preflight.py
```

Then require aggregate feature guards, deterministic smoke and the admitted locked-reference BricsCAD V25 plugin compile on the exact PR head. Hosted/source/build GREEN is not licensed runtime evidence.

## LOCAL_ONLY boundary
Real Teigha `Transaction.Dispose` / `DocumentLock.Dispose` failure timing, same-wrapper native database replacement, MDI switching and visible palette/editor warning behavior require licensed BricsCAD V25. Until an exact-SHA run exists, record these cells as `LOCAL_ONLY / NO_RESULT`.
