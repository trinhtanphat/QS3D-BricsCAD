# Work claim — Audit command error redaction

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-audit-command-error-redaction-20260812-1102`
- Registered: `2026-08-12T11:02:00+07:00`
- Baseline main SHA: `ad62c1648569c5ae792378bdaefc7325b3778f8e`
- Priority: owner-requested continue-all residual diagnostic privacy hardening

## Confirmed defect

`src/QS3D.BricsCAD.V25/AuditCommands.cs` previously caught `System.Exception ex` at the `QS3DAUDIT` command boundary and reflected `ex.Message` into both `Editor.WriteMessage(...)` and `PaletteCoordinator.SetStatus(...)`. Runtime exception messages could contain filesystem/provider/environment details and should not be exposed in user-visible diagnostics.

## Reserved scope

- Redact raw exception-message reflection from the `QS3DAUDIT` top-level catch.
- Preserve command registration, read-only project lookup, no-project side-effect-free behavior, modeless `AuditLogWindow` lifecycle, success status, and protected failure reporting to both Editor and Palette.
- Add one focused static regression preflight.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/AuditCommands.cs`
- `scripts/preflight-audit-command-error-redaction.py`
- this claim file

## Excluded scope

- No audit event semantics, persistence, window layout/content, project mutation, Actions dispatch, release publication, force push, build PASS, or BricsCAD runtime PASS claim.

## Validation completed

- Claim registration: `d703709b0f70aa6376996c488f75aa3c47acb361`.
- Source fix: `324968acfea46d348926395391d0a66f53ec03fd`.
- Focused preflight source: `6c04b2dd7f9c5e5b3986953df18bdba14c3e8c01`.
- Readback on current `main` confirmed `catch (System.Exception)` without an exception variable, stable generic Editor text `QS3DAUDIT error: không thể mở nhật ký thay đổi.`, and stable generic Palette status `Nhật ký thay đổi lỗi: không thể mở nhật ký thay đổi.`.
- Readback confirmed read-only `ProjectContextCoordinator.TryGetReadOnly(...)`, modeless `AuditLogWindow`, close cleanup, and no-project success status remain intact.
- Readback confirmed `scripts/preflight-audit-command-error-redaction.py` pins those contracts and rejects `catch (System.Exception ex)`, `ex.Message`, and exception-detail concatenation.
- Ancestry verification against `main` SHA `6c04b2dd7f9c5e5b3986953df18bdba14c3e8c01` confirmed the source fix is an ancestor and the focused preflight commit is current HEAD.
- Python preflight execution, GitHub Actions, build, and licensed BricsCAD V25/V26 runtime were not executed or claimed PASS through this connector session.

## Completion condition

Completed: current `main` no longer reflects exception messages from `QS3DAUDIT`, existing read-only/modeless behavior remains source-pinned, focused regression source exists, and exact integration evidence is recorded above.