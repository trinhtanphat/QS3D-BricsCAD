# Work claim — Release #38 Grid preflight reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release38-grid-preflight-reconcile-20260812-1539`
- Registered: `2026-08-12T15:39:00+07:00`
- Baseline main SHA: `2ddda3274ed6780aed12f039408056bc22f80508`
- Priority: P1 release preflight / stale structural contracts

## Confirmed mismatches

Release #38 reports both Grid gates as failures.

`preflight-grid-annotation-empty-handle-token.py` still requires `distinct.Add(handle)`, while `GeneratedGridAnnotationHealthService` now validates hex handles and canonicalizes numeric CAD identity before duplicate detection with `distinct.Add(identity)`. The stronger empty-token fail-visible behavior remains present.

`preflight-grid-naming-bounded-enumeration.py` forbids any `.ToList();` inside `Renumber`, but the current implementation intentionally snapshots `project.Elements.ToList()` before enumerating caller-controlled target IDs for input/project freshness. The ordered input itself remains manually bounded and rejects at the first item beyond 2,000; focused smoke still pins 2,001 yields and no mutation.

## Reserved scope

- `scripts/preflight-grid-annotation-empty-handle-token.py`
- `scripts/preflight-grid-naming-bounded-enumeration.py`
- this claim file

## Expected reconciliation

Pin the current stronger semantic contracts without weakening the aggregate dispatcher or production code: numeric CAD-handle identity for annotation duplicate detection, and bounded manual target enumeration while permitting the independent project-element snapshot.

## Excluded scope

- no production behavior changes;
- no revision timestamp / run #34 lane;
- no workflow-dispatch or failure suppression changes;
- no licensed runtime qualification claim.

## Completion condition

Both scripts are reconciled against current source/smoke contracts, integrated/read back on `main`, and this claim is closed with exact commit evidence.
