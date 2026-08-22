# Work claim — Material Catalog atomicity metadata gate reconciliation

- Status: `COMPLETED`
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

The static gate still required the superseded direct-clear token, so the gate was stale. Production metadata rollback remains present and was not reverted to satisfy a source-text literal.

## Reserved scope

- `scripts/preflight-material-catalog-atomicity.py`
- this claim document only

Reconciled only the stale snapshot metadata token contract with tokens that lock the current canonical metadata persistence restore. Preserved the existing Material Catalog Save/Delete/Apply atomicity, stale-row, whole-project rollback, post-commit UI isolation, AuditEvents and Elements restore checks.

## Explicit exclusions

- no changes to `ProjectStateSnapshot`, `ProjectMetadataDictionary`, `ProjectMaterialCatalog`, Material Catalog UI production source, semantic-mutation fixture, QSDB, native BricsCAD, LOCAL runners/probes, workflows, release, private data, or GitHub Actions;
- metadata restore coverage was not weakened;
- next independent failures remain separate claims.

## Validation

- branch implementation commit `f85f48a60b63ab8d00ce19bc40e7d9e863f21139` changed exactly one file with +3/-1;
- the stale `target.Metadata.Clear();` source-token requirement was replaced by locks for `target.Metadata as ProjectMetadataDictionary`, `targetMetadata.ReplacePersistenceState(source.Metadata);`, and `target.RestorePersistenceState(source.UpdatedUtc, source.ChangeVersion);`;
- existing `target.AuditEvents.Clear();` and `target.Elements.Clear();` coverage remained unchanged;
- no fresh local/full-suite/BricsCAD PASS is claimed by this closeout because this environment does not execute the repository's .NET suite.

## Completion record

- Claim-only PR `#1258` merged at `59638193b02e71bbd5ff35103d3b7a1245c8c5cc` before implementation.
- Implementation PR `#1263` merged to `main` at `544b0109a13825656c414931880d685799f5c008`.
- Intervening commits between implementation baseline and PR base did not touch `scripts/preflight-material-catalog-atomicity.py`.
- Production source was intentionally unchanged; the fix reconciles the static gate with the authoritative metadata rollback implementation already present in `ProjectStateSnapshot`.
