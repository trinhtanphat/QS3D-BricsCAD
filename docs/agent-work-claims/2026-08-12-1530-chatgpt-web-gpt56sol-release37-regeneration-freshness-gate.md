# Work claim — Release #37 regeneration dirty-subset freshness gate

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release37-regeneration-freshness-gate-20260812-1530`
- Registered: `2026-08-12T15:30:00+07:00`
- Baseline main SHA: `f5291f0be8d670f18d2929ac6752dae9b5effaa7`
- Priority: P1 release preflight / stale freshness gate

## Confirmed mismatch

`RegenerationEngine.RegenerateDirtySubset(...)` has been hardened beyond the old version-only materialization contract. Current source captures `inputVersion`, snapshots `project.Elements.ToArray()`, bounds target IDs against that captured cardinality, checks ChangeVersion, then calls `RequireElementStructureFresh(project, sourceElements)` before the zero-target no-op and again before regeneration. This catches same-count same-ID instance replacement that ChangeVersion alone cannot detect.

Release #37 `preflight-regeneration-dirty-subset-input-freshness.py` still requires the obsolete local name `materializeVersion` and does not pin structural ownership freshness. Its smoke-name literals also predate the current focused smoke method names.

## Reserved scope

- `scripts/preflight-regeneration-dirty-subset-input-freshness.py`
- this claim file

## Expected reconciliation

Pin the current stronger source contract: pre-enumeration project-element snapshot, bounded target materialization, ChangeVersion check, same-instance structural freshness before zero-target return, and current focused smoke names/registration. Do not change production regeneration behavior.

## Excluded scope

- no Core/source changes;
- no Source Reconcile service changes;
- no GitHub Actions rerun/dispatch;
- no runtime qualification claim.

## Completion condition

Gate is integrated/read back on `main` and the claim is closed with exact SHA evidence.
