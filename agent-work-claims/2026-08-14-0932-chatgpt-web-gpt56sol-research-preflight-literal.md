# Work claim — Research implementation preflight literal brittleness

- Status: `COMPLETED`
- Phase: `SOURCE_FIXED / REMOTE_VERIFIED / PENDING_FRESH_AGGREGATE`
- Agent: `chatgpt-web-gpt56sol-research-preflight-literal-20260814-0932`
- Registered: `2026-08-14T09:32:00+07:00`
- Baseline main SHA: `d03e519a116ea1ee17cf87f49c3110e4a4024559`
- Priority: `P0 aggregate-preflight regression` — the new research implementation guard must validate the archive/live-backlog boundary without requiring one editorial sentence form.

## Confirmed defect

A capable exact-main aggregate preflight run on `e98c30fb79abe41e0f9df6b5cd1d175152453675` reported `preflight-research-implementation-status.py` as one of four failing gates. Read-back of the guard and docs identified the brittle assertion: the gate required the exact index literal `not a live list of missing code`, while the index deliberately stated the same boundary as `Prevents the dated advisory queue from being mistaken for a live list of missing code.` The semantic contract was present; the source-shape test rejected a harmless wording variant.

## Reserved scope

- `scripts/preflight-research-implementation-status.py`
- this claim file

## Implemented acceptance

1. Both research entry points still have to link the implementation-status overlay.
2. The documented source-evidence files and status taxonomy remain guarded.
3. The exact editorial-sentence dependency was replaced with stable semantic markers for advisory classification, non-canonical research truth, dated queue status, provenance, and the live-backlog concept.
4. The gate still requires research to remain advisory/provenance rather than canonical live backlog.
5. No production/Core/native/release behavior changed.

## Explicit non-scope

No changes to product-boundary, V25 NETLOAD/update UX, wall-junctions, GitHub Actions, research archive provenance content, or source evidence domains.

## Completion record

- Claim-only commit: `1467affc6921099f2c2ad1d38c06905b86d7cb67`.
- Guard fix: `c093695b7b773b15d7671068341eb0edafe9df9f` (`fix(preflight): avoid brittle research wording literal`).
- Remote read-back verified the guard now requires `Advisory research/archive index`, `not canonical QS3D product truth`, `dated advisory queue`, `live list of missing code`, the research provenance marker, the explicit research non-live-backlog marker, the workstream advisory status, and the claim ownership boundary.
- Current `docs/research/BLT3D-GEMINI-RESEARCH-INDEX.md` contains the required advisory/non-canonical/dated/live-list concept markers; current `docs/BLT3D-RESEARCH.md` contains the required provenance and `not a live list of missing QS3D code` markers.
- Local execution: `NOT_RUN` in this connected GitHub environment.
- Fresh aggregate preflight: `PENDING_FRESH_AGGREGATE`; the earlier failure was on pre-fix SHA `e98c30fb79abe41e0f9df6b5cd1d175152453675`.
- GitHub Actions: `NOT_DISPATCHED`.

## Completion

Source fix is complete and remotely verified. A later capable runner should include `preflight-research-implementation-status.py` in its normal aggregate rerun; do not reuse the pre-fix aggregate failure as evidence against the corrected gate.
