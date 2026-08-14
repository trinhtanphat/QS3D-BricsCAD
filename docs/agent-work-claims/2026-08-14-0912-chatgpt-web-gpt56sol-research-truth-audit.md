# Work claim — Research implementation truth audit

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-research-truth-audit-20260814-0912`
- Registered: `2026-08-14T09:12:00+07:00`
- Baseline main SHA: `6fc7e9a8d2fa207905ea3f0d6fa0beab9965b3b9`
- Priority: `P1 documentation/research integrity` — archived advisory research must not be mistaken for unimplemented canonical product backlog after the corresponding Core foundations have landed.

## Confirmed repository gap

`docs/BLT3D-RESEARCH-TO-QS3D-AGENT-WORKSTREAMS-2026-08-12.md` correctly says it is an advisory queue, but the research archive/index had no current implementation-truth overlay. Current `main` already contains the foundational MTR/MAP/REV/CST source families (`MeasurementTrace`, measurement snapshot/delta/reason, measurement/work-item mapping + coverage, RateBook/EstimateLine/revision-cost/frozen projection). Without a current status map, later agents could read the dated advisory queue as missing work and duplicate an already-established domain.

## Reserved scope

- `docs/research/BLT3D-QS3D-IMPLEMENTATION-STATUS-2026-08-14.md` — evidence/status overlay.
- `docs/research/BLT3D-GEMINI-RESEARCH-INDEX.md` — link and boundary clarification only.
- `docs/BLT3D-RESEARCH.md` — link and clean-room/status clarification only.
- `scripts/preflight-research-implementation-status.py` — narrow static guard for the archive/status boundary.
- this claim file.

## Implemented acceptance

1. MTR/MAP/REV/CST foundational source evidence is recorded without claiming licensed BricsCAD runtime qualification.
2. The overlay distinguishes `SOURCE_IMPLEMENTED`, `FOUNDATION_PRESENT`, `PARTIAL_OR_OPEN`, `LOCAL_ONLY`, `ENGINEERING_REQUIRED`, `OUT_OF_SCOPE_OR_SEPARATE_PRODUCT`, and `ARCHIVE_ONLY` states.
3. Dated Gemini/public-source material remains provenance and is not rewritten as verified competitor implementation fact.
4. Remaining research ideas are explicitly not automatic code TODOs; current-source verification and claim-first ownership remain mandatory.
5. `scripts/preflight-research-implementation-status.py` is auto-discovered by `preflight-all.py` and checks overlay/index/boundary markers plus the documented source-evidence files.
6. No production source, Selection, wall, update/release, native/local runner, geometry, reporting or cost behavior was modified by this lane.

## Completion record

- Claim-only commit: `608d66195a2a532b73e5a85f326a876bd52ca1d6`.
- Implementation-truth map: `1a0f76e5900e40acb2e79320182f92b39576a7ed`.
- Research archive index link/boundary update: `5f3392faa4e32ebf657eedb06f15124a25c69e1f`.
- Public BLT3D research entry-point cleanup: `8afcec43557d7ce47d4dcb24d7491a366af753f3`.
- Static regression guard: `f11488e81f73ac4454b637e2fa4bd5660e90d85e`.
- Remote read-back: the pushed status map and preflight were re-fetched from live `main`; expected text/source evidence references are present.
- `preflight-all.py` discovery contract was re-read and confirms every `scripts/preflight-*.py` file except itself is auto-discovered.
- Local/preflight execution: `NOT_RUN` — the connected GitHub workflow provides repository reads/writes but no mounted checkout/toolchain for executing the new gate locally.
- GitHub Actions: `NOT_DISPATCHED` — this documentation/research integrity lane did not require or authorize a workflow dispatch.
- BricsCAD native runtime: `NOT_RUN` / not applicable to this docs/static lane.

## Completion

Satisfied. Research provenance remains intact, current implementation truth is visible, already-landed MTR/MAP/REV/CST foundations are no longer easy to mistake for absent code, broader/local/engineering/out-of-scope items remain truthfully open, and the new static preflight guards that boundary without changing production behavior.
