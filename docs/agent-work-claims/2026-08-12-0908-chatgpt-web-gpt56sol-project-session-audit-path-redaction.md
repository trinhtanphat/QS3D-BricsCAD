# Work claim — ProjectSession audit path redaction

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-project-session-audit-path-redaction-20260812-0908`
- Registered: `2026-08-12T09:08:00+07:00`
- Baseline main SHA: `55301299f8878eee87ef447aa110bb98cd01af73`
- Priority: P1 — persisted audit history must not leak absolute local/network QSDB paths.
- Task Key: `CORE-PROJECT-SESSION-AUDIT-PATH-REDACTION`

## Confirmed defect

`ProjectSession.Save()` and `Reload()` currently record the session's absolute `Path` as `AuditEvent.Detail` for `PROJECT_SAVE` / `PROJECT_RELOAD`. `QsdbProjectStore.Serialize(...)` persists `AuditEvent.Detail` verbatim into the QSDB `<audit>` event attributes. A normal session save can therefore embed host-specific filesystem information such as `C:\Users\<name>\...`, temporary directories, mounted shares, or UNC/network paths into project history. Existing ProjectSession recovery tests require the action records and atomic lifecycle but do not require raw path detail.

## Reserved scope

- `src/QS3D.Core/Services/ProjectSession.cs`
- `tests/QS3D.Core.SmokeTests/ProjectSessionAuditPathRedactionSmoke.cs`
- this claim file

## Intended contract

- `PROJECT_SAVE` and `PROJECT_RELOAD` audit events retain their action names but use empty diagnostic detail instead of the absolute session path.
- Save/reload atomicity, backup-recovery provenance, audit binding, lock requirements and successful event counts remain unchanged.
- Persisted QSDB audit must contain no session path through these events.
- No generic AuditTrail schema/policy, QSDB file path semantics, UI/native BricsCAD or recovery behavior changes.

## Validation plan

Focused auto-registered Core smoke creates a session under a distinctive temporary path, performs Save + Reload + Save, verifies in-memory and persisted PROJECT_SAVE/PROJECT_RELOAD events have empty Detail and do not contain the path, while action counts remain intact. Re-fetch current source/claim before writes. No force-push, Actions dispatch or BricsCAD runtime qualification claim.
