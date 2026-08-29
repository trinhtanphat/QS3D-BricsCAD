# ACTIVE — LOCAL-021 exact preview 10248 licensed runtime

- Owner/lane: `/root`, issue `#4041`, `LOCAL-021` (Móng Bè / Quantity Insight), `LOCAL_ONLY`.
- Scope: one bounded BricsCAD V25 x64 atomic interactive qualification using the canonical 65-stage raft runner and native physical mouse/keyboard/UI Automation. No V26 run is included in this claim.
- Exact source: tag `v0.1.0-preview.10248`, SHA `94ea7eab9a348d9402fe8b6d1ee05614eabc39ce`.
- Exact package: official V25 ZIP SHA-256 `78C6F0A7FEB1CDD1991B5557FC524D888A7F8DC3223C774B84F82C17781A7212`; package adapter SHA-256 `C6AEB3B462FF21F446B0BDEB4D081ACF4E32535390F1B4FC474CACB612159D3E`; package Core SHA-256 `BFE4C9B55139520CCC005FE007F6E8B6CEB692CE1E9FCA2028AEDD2A9AB8C2A8`.
- Offline admission: `scripts/preflight-all.py` discovered `1214` gates and passed; Core Release build passed `0 warnings / 0 errors`; V25 Release build passed `0 warnings / 0 errors`. V26 exact build is blocked by CS0649 in linked `UpdateCenterWindow` and is intentionally not claimed as runtime evidence.
- Runtime status: `V25_RUNTIME_PENDING`; no `LOCAL_PASS` until every preregistered stage, exact identity, save/fresh-process reopen, screenshots, and cleanup assertions pass. A startup stop is `NO_RESULT`; a later assertion failure is `RUNTIME_FAIL`; host contention is `NO_RESULT / HOST_CONTENTION`.
- Host safety: take a fresh three-sample `bricscad.exe=0` and `cloudflared.exe=0` preflight immediately before launch; reserve the shared host exclusively for this cell; do not inspect, attach to, or terminate another agent's process. Restore the owned profile, DemandLoad/Loader state, UI and disposable DWG, then prove stable zero process before sending `HOST_RELEASED`.
- Evidence boundary: all raw/private harness output remains under ignored `artifacts/`; only sanitized outcome metadata may be committed. This claim does not authorize a main merge or reuse any consumed `.10230`/`.10233` allocation.
- Source handoff: the V26 CS0649 build blocker is source-safe work for a remote/source lane; this local claim does not patch it.

Updated: 2026-08-29
