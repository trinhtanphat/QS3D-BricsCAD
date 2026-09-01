# LOCAL_ONLY V25 qualification — Right Panel Xref lock/unlock failure isolation

Issue: #5150  
Lane-Key: issue-5150  
Status: `SOURCE_READY / NO_RESULT / LOCAL_ONLY`

This matrix is prepared for a licensed BricsCAD V25 Windows x64 local agent after the exact candidate SHA is pushed. Remote/static CI must not be reported as `LOCAL_PASS`.

## Exact-artifact rules

1. Fetch the exact candidate SHA recorded on Issue #5150 / PR #5151 (or its eventual canonical PR).
2. Build/load that exact V25 plugin artifact only; record DLL SHA-256 and BricsCAD V25 version.
3. Use a disposable DWG containing a known attached Xref with at least one live instance layer.
4. Do not reinterpret static/preflight PASS as licensed runtime evidence.
5. Record editor/palette/right-panel output verbatim enough to establish stable user-visible behavior, but do not publish private paths or customer DWG identifiers.

## Matrix

- **XL01 — lock success:** select the Xref in Right Panel, lock instance layers, verify affected layers become locked and the success status reports the affected count.
- **XL02 — unlock success:** unlock the same Xref instance layers and verify the affected layers become unlocked with the correct success status.
- **XL03 — zero-instance success:** exercise an Xref definition with no instance in the current space and verify the zero-change status; no error status is expected.
- **XL04 — host failure redaction (lock):** induce a safe native failure before mutation and verify the status is exactly the stable lock failure message, with no exception type/message/path/stack detail.
- **XL05 — host failure redaction (unlock):** repeat for unlock and verify the stable unlock failure message with no raw host detail.
- **XL06 — primary failure recovery:** after XL04/XL05, verify the drawing and layer lists are refreshed best-effort and remain usable.
- **XL07 — secondary refresh failure:** safely induce a refresh/reload failure after the primary operation failure and verify the original stable failure status is retained with only the generic refresh warning suffix.
- **XL08 — no exception escape:** for primary and secondary failures, verify no exception escapes to BricsCAD command processing or tears down the modeless panel.
- **XL09 — active-DWG behavior:** switch the active DWG before invoking the action and verify the operation targets the currently active document consistent with existing Right Panel semantics.
- **XL10 — selection preservation:** after a recoverable failure, verify Right Panel remains responsive and a subsequent valid Xref can be selected and mutated.
- **XL11 — repeated cycle:** perform lock/unlock success → induced failure → success again and verify no stale error state prevents later operations.
- **XL12 — cleanup:** close the test drawing/panel, verify no owned UI/resource remains stuck, and restore the disposable test environment.

## PASS boundary

`LOCAL_PASS` requires all XL01–XL12 on the exact recorded candidate artifact with artifact identity and BricsCAD version captured. Any skipped or unexecuted cell remains `NO_RESULT`; source/preflight/CI evidence alone is never sufficient.
