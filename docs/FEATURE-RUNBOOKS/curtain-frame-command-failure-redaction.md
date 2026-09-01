# Curtain Frame command failure redaction — V25 qualification

Status: SOURCE_READY / LOCAL_ONLY_RUNTIME / NO_RESULT

Issue: #5247
Lane-Key: `issue-5247`

This runbook qualifies the standalone `QS3DCURTAINFRAMES3D` failure, live-fingerprint pending-warning and post-commit presentation boundary. Hosted/static CI can prove source guards and locked-reference compilation only; it is not licensed BricsCAD runtime evidence.

## Source contract

- read current selection before project mutation admission;
- require an existing QS3D project and preserve the current line-frame then path-frame native orchestration;
- preserve generated ownership/rollback authority inside the existing builders;
- fingerprint stamping remains post-generation metadata: failure leaves native frame success intact and emits only a stable pending warning;
- caught command/native/UI exceptions are never copied to Palette or Editor surfaces;
- Palette refresh, Editor Regen and status publishing are independent best-effort post-generation work;
- failure of those UI operations may only add the stable “native update completed; UI could not fully synchronize” warning.

## LOCAL_ONLY matrix

Bind every executed row to one exact pushed SHA, adapter/Core ProductVersion and SHA-256, licensed BricsCAD V25 identity, and a disposable/sanitized DWG.

| ID | Scenario | Required result |
|---|---|---|
| CF01 | No implied selection | Stable selection guidance; no project/native mutation. |
| CF02 | Valid GlassWall LINE | Line curtain frames generated/replaced with expected ownership; no raw host detail. |
| CF03 | Valid open straight POLYLINE | Path curtain frames generated/replaced and fingerprint stamped. |
| CF04 | Valid bulged open POLYLINE | Tessellated path frames generated according to existing path contract. |
| CF05 | Missing/invalid existing project or unsupported selection | Stable fail-closed command result; no raw exception text or partial new ownership. |
| CF06 | Builder/native failure before commit | Existing rollback/fail-closed semantics preserved; command reports only stable failure. |
| CF07 | Fingerprint stamp failure after valid frame generation | Frames remain valid; `fingerprint pending` plus stable remediation warning; no exception detail. |
| CF08 | Palette refresh failure after native success | Native output remains successful; stable UI-sync warning only. |
| CF09 | Editor Regen failure after native success | Native output remains successful; status/reporting remains best-effort. |
| CF10 | Palette status or Editor write failure | Presentation failure is swallowed/fail-isolated and cannot redefine native result. |
| CF11 | Save + cold reopen | Generated ownership/fingerprint health remains coherent for the original drawing. |
| CF12 | Two-DWG isolation + repeated rebuild | No cross-document ownership/state bleed; replacement remains deterministic for each DWG. |

## Evidence rules

Record sanitized exact-SHA evidence only: Git SHA, ProductVersion, binary hashes, BricsCAD version, row verdict, bounded observations and cleanup state. Do not publish private DWG contents, paths/secrets, stack traces or raw exception messages.

A row may be called `LOCAL_PASS` only after actual licensed execution on the exact bound candidate. Source guards, Core smoke and V25 locked-reference compile remain `REMOTE_SAFE` evidence.
