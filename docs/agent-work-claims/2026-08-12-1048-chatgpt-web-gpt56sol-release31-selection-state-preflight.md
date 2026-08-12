# Work claim — release #31 SelectionState preflight reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release31-selection-state-preflight`
- Registered: `2026-08-12T10:48:00+07:00`
- Baseline main SHA: `2bc308fcbf397a775ea7e54beb04630e02973d99`
- Priority: release #31 reports `scripts/preflight-selection-state.py` failing after SelectionState replacement moved from an unbounded LINQ normalization pipeline to bounded/freshness-aware enumeration.

## Reserved scope

Reconcile only `scripts/preflight-selection-state.py`. Preserve SelectionState and smoke production/test behavior unchanged.

## Canonical evidence

- Replace rejects known collections above 10,000 entries and stops lazy input at entry 10,001.
- Each enumerated id still ignores whitespace-only values and adds `raw.Trim()` into an OrdinalIgnoreCase HashSet.
- Replacement pins `_changeVersion` before enumeration and fails if selection state changes during enumeration.
- Canonically equivalent replacement remains a no-op and Clear remains a no-op when already empty.
- Existing smoke still covers trim/dedup/blank/no-op clear behavior.

## Excluded scope

No Core/test source edits, no weakening of normalization/bounds/freshness/no-op behavior, and no unrelated #31 work.

## Completion condition

The gate follows the stronger bounded/freshness implementation while preserving canonical selection semantics, is pushed to `main`, and this claim is closed with exact evidence.