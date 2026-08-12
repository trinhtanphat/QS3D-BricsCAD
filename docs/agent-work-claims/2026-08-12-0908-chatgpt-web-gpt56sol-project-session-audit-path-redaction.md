# Work claim — ProjectSession audit path redaction

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-project-session-audit-path-redaction-20260812-0908`
- Registered: `2026-08-12T09:08:00+07:00`
- Completed: `2026-08-12T09:10:00+07:00`
- Baseline main SHA: `55301299f8878eee87ef447aa110bb98cd01af73`
- Claim commit: `4bcfa85b38a96a252c5fbe8b4bcef418ac6a0767`
- Source fix commit: `425d850e6b31eb4477cd2984cbef963bf949073e`
- Focused smoke commit: `4f0eae982ac88e2fe7a398d18d2afde1cf4c3669`
- Priority: P1 — persisted audit history must not leak absolute local/network QSDB paths.
- Task Key: `CORE-PROJECT-SESSION-AUDIT-PATH-REDACTION`

## Confirmed defect

`ProjectSession.Save()` and `Reload()` recorded the absolute session `Path` in `AuditEvent.Detail`, and `QsdbProjectStore.Serialize(...)` persists audit Detail verbatim. Successful session operations could therefore embed host-specific filesystem, username, temporary-directory, mounted-share or UNC path information into project history.

## Implemented contract

- `PROJECT_SAVE` and `PROJECT_RELOAD` retain their existing action names but now record empty Detail.
- Save/reload lock requirements, audit event counts, atomic rollback, binding and backup-recovery provenance remain unchanged.
- No generic AuditTrail schema/policy, QSDB path semantics, UI/native BricsCAD or recovery behavior was modified.

## Validation evidence

- Current `main` readback confirms both session audit calls pass `string.Empty` as Detail.
- `ProjectSessionAuditPathRedactionSmoke` is auto-registered and performs Save → Reload → Save under a distinctive temporary path, verifying in-memory and persisted QSDB action counts plus empty Detail for all PROJECT_SAVE/PROJECT_RELOAD events.
- The existing ProjectSession recovery smoke remains untouched and authoritative for recovery lifecycle semantics.
- This connector-only session did not execute .NET smoke, GitHub Actions or licensed BricsCAD runtime tests.

## Completion

`COMPLETED`: ProjectSession no longer persists absolute QSDB session paths through save/reload audit details.
