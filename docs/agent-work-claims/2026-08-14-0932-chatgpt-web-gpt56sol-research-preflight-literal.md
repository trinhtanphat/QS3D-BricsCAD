# Work claim — Research implementation preflight literal brittleness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-research-preflight-literal-20260814-0932`
- Registered: `2026-08-14T09:32:00+07:00`
- Baseline main SHA: `d03e519a116ea1ee17cf87f49c3110e4a4024559`
- Priority: `P0 aggregate-preflight regression` — the new research implementation guard must validate the archive/live-backlog boundary without requiring one editorial sentence form.

## Confirmed defect

A capable exact-main aggregate preflight run on `e98c30fb79abe41e0f9df6b5cd1d175152453675` reported `preflight-research-implementation-status.py` as one of four failing gates. Read-back of the current guard and docs identifies the brittle assertion: the gate requires the exact index literal `not a live list of missing code`, while the index deliberately states the same boundary as `Prevents the dated advisory queue from being mistaken for a live list of missing code.` The semantic contract is present; the source-shape test rejects a harmless wording variant.

## Reserved scope

- `scripts/preflight-research-implementation-status.py`
- this claim file

## Acceptance

1. Keep checking that both research entry points link the implementation-status overlay.
2. Keep checking the source-evidence files and status taxonomy.
3. Replace the exact editorial-sentence dependency with stable semantic boundary markers already present in the docs.
4. Do not weaken the requirement that research is advisory/provenance rather than canonical live backlog.
5. No production/Core/native/release behavior changes.

## Explicit non-scope

No changes to product-boundary, V25 NETLOAD/update UX, wall-junctions, GitHub Actions, research archive provenance content, or source evidence domains.

## Validation plan

Publish this claim alone, refresh `main`, patch only the brittle assertions, re-fetch the exact guard/docs, and close `COMPLETED`. A fresh capable aggregate-preflight rerun remains separate evidence; no GitHub Actions dispatch is authorized.
