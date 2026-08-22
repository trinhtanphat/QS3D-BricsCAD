# Work claim — Research implementation truth audit

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-research-truth-audit-20260814-0912`
- Registered: `2026-08-14T09:12:00+07:00`
- Baseline main SHA: `6fc7e9a8d2fa207905ea3f0d6fa0beab9965b3b9`
- Priority: `P1 documentation/research integrity` — archived advisory research must not be mistaken for unimplemented canonical product backlog after the corresponding Core foundations have landed.

## Confirmed repository gap

`docs/BLT3D-RESEARCH-TO-QS3D-AGENT-WORKSTREAMS-2026-08-12.md` correctly says it is an advisory queue, but the research archive/index has no current implementation-truth overlay. Current `main` already contains the foundational MTR/MAP/REV/CST source families (`MeasurementTrace`, measurement snapshot/delta/reason, measurement/work-item mapping + coverage, RateBook/EstimateLine/revision-cost/frozen projection). Without a current status map, later agents can read the dated advisory queue as missing work and duplicate an already-established domain.

## Reserved scope

- `docs/research/BLT3D-QS3D-IMPLEMENTATION-STATUS-2026-08-14.md` — new evidence/status overlay.
- `docs/research/BLT3D-GEMINI-RESEARCH-INDEX.md` — link and boundary clarification only.
- `docs/BLT3D-RESEARCH.md` — link and clean-room/status clarification only.
- `scripts/preflight-research-implementation-status.py` — narrow static guard for the archive/status boundary.
- this claim file.

## Acceptance

1. Record source evidence for MTR, MAP, REV and CST foundations without claiming native BricsCAD runtime qualification.
2. Distinguish `SOURCE_IMPLEMENTED`, `PARTIAL/OPEN`, `LOCAL_ONLY`, `ENGINEERING_REQUIRED`, `OUT_OF_SCOPE/SEPARATE_PRODUCT`, and advisory/archive-only material.
3. Keep dated Gemini/public-source research as provenance; do not rewrite it as verified competitor fact.
4. Explicitly state that remaining research ideas are not automatically code TODOs and still require current-source verification + claim-first ownership.
5. Add an auto-discovered preflight that prevents deletion of the implementation map/boundary links and verifies the evidence files used by the completion matrix still exist.
6. No production source, Selection, wall, update/release, native/local runner, geometry, reporting or cost behavior changes in this lane.

## Validation plan

Publish this claim alone, refresh `main`, recheck concurrent claims, add the bounded status docs + static preflight, verify the pushed files through GitHub, and close this claim `COMPLETED`. Managed/native execution will be reported truthfully; no GitHub Actions dispatch is part of this lane.
