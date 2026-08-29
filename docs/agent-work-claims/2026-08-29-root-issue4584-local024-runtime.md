# LOCAL-024 — #4584 OAuth/full-agent runtime cell

**Status:** ACTIVE / LOCAL_ONLY / V25 FIRST  
**Updated:** 2026-08-29 (UTC+7)  
**Lane-Key:** `issue-4584-local024-runtime`  
**Parent:** #4352 / LOCAL-024  
**OAuth extension:** #4584  
**Branch:** `agent/root/issue-4584-local024-runtime`

## Exact candidate

- Published preview: `v0.1.0-preview.10250`
- Source SHA: `d3e6e4d6e9f423efbd2d236d600110a6a2ced1f5`
- Official V25 package SHA-256: `8FEA4804EA14EF106AD230338F441C8801F40FCB7FFD36AABCD2F2C239E0DF5D`
- V25 adapter SHA-256: `00649085701B3A122F344A3E0606DE3CC95F6D544A2764187C4D93BA05446F10`
- Core SHA-256: `CA343909E93CC033E96129C8A3D27B6BB6740AE625B39A2F492FA07C171F8C31`

## Bounded scope

Run one exclusive licensed BricsCAD V25 cell using the unchanged runtime loader/probe, then exercise the #4584/LOCAL-024 MCP contract: embedded loopback health and read-only protocol probe, Agent Center/self-test, OAuth discovery/DCR/PKCE/consent/replay/refresh/resource binding where the local account/browser state permits, legacy bearer compatibility, one confirmed disposable-DWG mutation with stop/cancel/recovery, save/reopen and clean shutdown. V26 is a separate lane and may only be reported if a source-clean V26 binary is available.

The additive holder `scripts/test-mcp-v25-local-cell.ps1` keeps that single exact
NETLOAD host alive after the canonical generic V25 probe, waits for loopback MCP,
and records only sanitized readiness/cleanup fields. It is a local harness, not a
replacement for the canonical runtime runner.

## Safety and evidence boundary

- Fresh three-sample `bricscad.exe=0` and `cloudflared.exe=0` is required before reservation.
- Stop on any host contention; do not inspect, attach, or terminate another lane's process.
- Restore profile, DemandLoad/Loader, tunnel ownership, drawing and all task-owned residue; prove stable zero before release.
- Publish only sanitized evidence. Never record tokens, credentials, private paths/DWGs, proprietary DLLs, raw handles or unsanitized screenshots.
- Runtime outcome must be classified `LOCAL_PASS`, `PARTIAL_LOCAL_PASS`, `RUNTIME_FAIL`, or `NO_RESULT`; static/build evidence never substitutes for licensed runtime proof.

## Current result

`PENDING_LOCAL` — registration and holder preparation complete; host reservation and runtime cell not yet started.
