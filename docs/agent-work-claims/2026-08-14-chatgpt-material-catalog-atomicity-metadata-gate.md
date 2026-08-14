# Work claim — Material Catalog atomicity metadata gate reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-14T16:06:00+07:00`
- Baseline main SHA: `5b73758ad4a3a0ae3e50f8f782fd96a6ec3a03c1`
- Priority: independent deterministic static-gate failure reported by Material Catalog validation after the raw FamilyId regression

## Confirmed gate drift

PR #1253 reports `preflight-material-catalog-atomicity.py` failing on unchanged `ProjectStateSnapshot` token `target.Metadata.Clear();` while the Material Catalog focused behavior and Core build otherwise pass.

Current `ProjectStateSnapshot.CopyInto(...)` restores metadata through the canonical `ProjectMetadataDictionary` persistence boundary:

- cast `target.Metadata as ProjectMetadataDictionary` fail-closed;
- `targetMetadata.ReplacePersistenceState(source.Metadata);`
- `target.RestorePersistenceState(source.UpdatedUtc, source.ChangeVersion);`

The static gate still requires the superseded direct-clear token, so the gate is stale. Production metadata rollback remains present and should not be reverted to satisfy a source-text literal.

## Reserved scope

- `scripts/preflight-material-catalog-atomicity.py`
- this claim document only

Replace only the stale snapshot metadata token contract with tokens that lock the current canonical metadata persistence restore. Preserve the existing Material Catalog Save/Delete/Apply atomicity, stale-row, whole-project rollback, post-commit UI isolation, AuditEvents and Elements restore checks.

## Explicit exclusions

- no changes to `ProjectStateSnapshot`, `ProjectMetadataDictionary`, `ProjectMaterialCatalog`, Material Catalog UI production source, semantic-mutation fixture, QSDB, native BricsCAD, LOCAL runners/probes, workflows, release, private data, or GitHub Actions;
- do not weaken the gate by deleting metadata restore coverage;
- report any next independent failure without expanding this claim.

## Validation

- exact diff/readback of the one-file script change;
- `preflight-material-catalog-atomicity.py` should pass under the canonical `ReplacePersistenceState` implementation when run in an environment with Python;
- no full-suite or BricsCAD PASS is inferred from this static reconciliation alone.

## Completion record

Pending implementation after this claim is merged to `main`.
