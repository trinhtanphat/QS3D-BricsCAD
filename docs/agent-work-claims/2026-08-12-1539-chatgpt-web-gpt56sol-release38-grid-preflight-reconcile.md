# Work claim — Release #38 Grid preflight reconciliation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-release38-grid-preflight-reconcile-20260812-1539`
- Registered: `2026-08-12T15:39:00+07:00`
- Baseline main SHA: `2ddda3274ed6780aed12f039408056bc22f80508`
- Priority: P1 release preflight / stale structural contracts

## Confirmed mismatches

Release #38 reports both Grid gates as failures.

`preflight-grid-annotation-empty-handle-token.py` still required `distinct.Add(handle)`, while `GeneratedGridAnnotationHealthService` validates hex handles and canonicalizes numeric CAD identity before duplicate detection with `distinct.Add(identity)`. The stronger empty-token fail-visible behavior remains present.

`preflight-grid-naming-bounded-enumeration.py` forbade any `.ToList();` inside `Renumber`, but the current implementation intentionally snapshots `project.Elements.ToList()` before enumerating caller-controlled target IDs for input/project freshness. The ordered input itself remains manually bounded and rejects at the first item beyond 2,000; focused smoke pins 2,001 yields and no mutation.

## Reserved scope

- `scripts/preflight-grid-annotation-empty-handle-token.py`
- `scripts/preflight-grid-naming-bounded-enumeration.py`
- this claim file

## Integrated reconciliation

- Claim: `baac54db89ff8aada0926c5bcf7c5e8c9ab7b8b2`
- Grid annotation preflight: `515cc544ce2ae9bec6f23f45b9863212178b574a`
- Grid naming preflight: `9716e5065e0a1e798d0b8b355f3236000842cd5b`

The annotation gate now pins empty-token visibility together with canonical numeric CAD-handle duplicate identity. The naming gate now distinguishes the legitimate project-element snapshot from forbidden eager materialization of caller target input, and pins enumeration/freshness/resolution ordering.

## Excluded scope

- no production behavior changes;
- no revision timestamp / run #34 lane;
- no workflow-dispatch or failure suppression changes;
- no licensed runtime qualification claim.

## Readback

Both updated preflight files were read back from `main` after their writes and match the intended current production/smoke contracts.
