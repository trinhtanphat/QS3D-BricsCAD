# Work claim — Release #37 regeneration dirty-subset freshness gate

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-release37-regeneration-freshness-gate-20260812-1530`
- Registered: `2026-08-12T15:30:00+07:00`
- Baseline main SHA: `f5291f0be8d670f18d2929ac6752dae9b5effaa7`
- Priority: P1 release preflight / stale freshness gate

## Confirmed mismatch

`RegenerationEngine.RegenerateDirtySubset(...)` has been hardened beyond the old version-only materialization contract. Current source captures `inputVersion`, snapshots `project.Elements.ToArray()`, bounds target IDs against that captured cardinality, checks ChangeVersion, then calls `RequireElementStructureFresh(project, sourceElements)` before the zero-target no-op and again before regeneration. This catches same-count same-ID instance replacement that ChangeVersion alone cannot detect.

Release #37 `preflight-regeneration-dirty-subset-input-freshness.py` still required the obsolete local name `materializeVersion` and did not pin structural ownership freshness. Its smoke-name literals also predated the current focused smoke method names.

## Integrated reconciliation

- Claim: `29ddfc3339ffbb576ffa198f76ceb0ceed67e294`
- Gate fix: `0ec5183ae29d6b63fd94f1bc8ed90eb49b210e49`

The gate now requires `inputVersion`, a captured `sourceElements` instance snapshot, bounded `CanonicalTargetIds(elementIds, sourceElements.Length)`, ChangeVersion freshness, `RequireElementStructureFresh(project, sourceElements)`, reference-identity verification, and both checks before the zero-target no-op. It also pins the current focused smoke scenario names and registration.

## Limitations

- Production `RegenerationEngine` was not changed by this lane.
- Source Reconcile gates were not modified by this claim.
- GitHub Actions were not rerun or dispatched.
- No aggregate build/package/runtime PASS is claimed.
