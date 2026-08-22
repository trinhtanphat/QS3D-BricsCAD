# Work claim — Curtain Frame Health command error redaction

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-curtain-frame-health-command-error-redaction-20260812-1035`
- Registered: `2026-08-12T10:35:00+07:00`
- Baseline main SHA: `1659644d824add4776759a92d89ab7489053e638`
- Priority: owner-requested continue-all residual diagnostic privacy hardening

## Confirmed defect

`src/QS3D.BricsCAD.V25/CurtainWallFrameHealthCommands.cs` previously caught `System.Exception ex` at the `QS3DCURTAINFRAMEHEALTH` command boundary and constructed `"QS3DCURTAINFRAMEHEALTH lỗi: " + ex.Message`, then wrote it to both Palette status and Editor output. Raw exception details could expose filesystem/provider/environment information.

## Reserved scope

- Redact raw exception-message reflection from the `QS3DCURTAINFRAMEHEALTH` top-level catch.
- Preserve command registration, read-only project access, frame/panel live-handle health aggregation, runtime health aggregation, health summary, modeless window, locate/select/zoom behavior, and both output sinks.
- Add one focused static regression preflight for this command boundary.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/CurtainWallFrameHealthCommands.cs`
- `scripts/preflight-curtain-frame-health-command-error-redaction.py`
- this claim file

## Excluded scope

- No changes to curtain frame/panel health service semantics, handle parsing, generation, project persistence, Actions dispatch, release publication, force push, or BricsCAD runtime PASS claim.

## Validation completed

- Claim registration: `20ca5e0f126c0bbabd26a049760fec115a70cadc`.
- Source fix: `b25480b8368cac03d637aee7cd2659c6d8c878f5`.
- Focused preflight source: `bb546d3df2b038b7c435117059f69a8ea6fba6a4`.
- Readback on current `main` confirmed `catch (System.Exception)` and stable generic text `QS3DCURTAINFRAMEHEALTH lỗi: không thể hoàn tất health check.` while preserving live solid collection, frame live-state inspection, panel health/live-state/runtime aggregation, modeless review, locate/select/zoom behavior, and Palette/Editor outputs.
- Readback confirmed `scripts/preflight-curtain-frame-health-command-error-redaction.py` pins those source contracts and rejects `catch (System.Exception ex)`, `ex.Message`, and exception-detail concatenation.
- Ancestry verification against `main` SHA `f3342a8990ae9396457100ef4cba8c3d09ff9ada` confirmed both source fix and focused preflight commit are ancestors.
- Python preflight execution, GitHub Actions, build, and licensed BricsCAD V25/V26 runtime were not executed or claimed PASS through this connector session.

## Completion condition

Completed: current `main` no longer reflects `ex.Message` from `QS3DCURTAINFRAMEHEALTH`, focused regression source pins the existing command flow, and exact integration evidence is recorded above.