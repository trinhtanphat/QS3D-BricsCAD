# Work claim — Rebar Health All command error redaction

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-rebar-health-all-command-error-redaction-20260812-1059`
- Registered: `2026-08-12T10:59:00+07:00`
- Baseline main SHA: `5711c367edc1754c993988470f904fd2bd902074`
- Priority: owner-requested continue-all residual diagnostic privacy hardening

## Confirmed defect

`src/QS3D.BricsCAD.V25/RebarHealthAllCommands.cs` catches `System.Exception ex` at the `QS3DREBARHEALTHALL` command boundary and constructs `"QS3DREBARHEALTHALL lỗi: " + ex.Message`, then writes it to Palette status and Editor output. Raw exception details can expose filesystem/provider/environment information.

## Reserved scope

- Redact raw exception-message reflection from the `QS3DREBARHEALTHALL` top-level catch.
- Preserve command registration, read-only project access, all generated rebar handle/live-solid collection, longitudinal/shape/tie/stirrup/slab/wall/foundation health aggregation, ownership/fabrication/BBS inspection, modeless review, issue-specific handle routing, locate/select/zoom behavior, and both output sinks.
- Add one focused static regression preflight for this command boundary.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/RebarHealthAllCommands.cs`
- `scripts/preflight-rebar-health-all-command-error-redaction.py`
- this claim file

## Excluded scope

- No generated rebar health semantics, handle routing policy, generation/persistence, BBS behavior, Actions dispatch, release publication, force push, or BricsCAD runtime changes.

## Validation plan

- Re-fetch current source after claim registration before editing.
- Replace only raw exception-message composition with a stable generic command failure message.
- Add a focused Python source preflight that rejects `ex.Message` and pins representative aggregation plus modeless/handle-routing/select/zoom/output contracts.
- Re-fetch source/preflight from current `main`, verify ancestry/readback, then close with exact SHAs.

## Completion condition

Completed only when current `main` no longer reflects `ex.Message` from `QS3DREBARHEALTHALL`, focused regression source pins the existing aggregator flow, and this claim is `COMPLETED` with exact integration evidence.