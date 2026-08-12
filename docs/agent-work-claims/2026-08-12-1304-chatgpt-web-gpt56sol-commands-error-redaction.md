# Work claim — Commands error redaction

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-commands-error-redaction-20260812-1304`
- Registered: `2026-08-12T13:04:00+07:00`
- Baseline main SHA: `66dbf414721d774ec2b19a809278c401e8683ad0`
- Priority: owner-requested continue-all residual command diagnostic privacy hardening

## Confirmed defect

`src/QS3D.BricsCAD.V25/Commands.cs` still reflects raw runtime exception messages through three shared user-visible command paths: `Guard(...)`, `FinalizeExportUi(...)`, and `FinalizeCommittedUi(...)`. These messages can expose filesystem/provider/environment detail in the BricsCAD Editor or Palette.

## Reserved scope

- Remove raw exception-message reflection from the three shared reporting helpers.
- Preserve explicit user-actionable validation messages by distinguishing QS3D-authored command validation from unexpected runtime failures rather than blindly hiding every authored BLOCKED reason.
- Preserve command registration, read-only/detached export behavior, post-commit best-effort UI semantics, and existing Palette/Editor sinks.
- Add a focused static regression preflight.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Commands.cs`
- `scripts/preflight-commands-error-redaction.py`
- this claim file

## Excluded scope

- No Core exporter changes, quantity/rebar calculation changes, UI layout redesign, Actions dispatch, release publication, force push, build PASS, or BricsCAD runtime PASS claim.

## Validation plan

- Re-fetch current source after claim registration before editing.
- Keep intentional QS3D validation reasons user-visible through an explicit safe validation type/path; unexpected exceptions must use stable generic failure text.
- Make post-export/post-commit UI warnings generic while keeping them best-effort and non-throwing.
- Add a focused Python source preflight that rejects `ex.Message` / `uiError.Message` in the shared reporters and pins the safe-validation/generic-failure split.
- Re-fetch source/preflight from current `main`, verify commit ancestry/readback, then close with exact SHAs.

## Completion condition

Completed only when current `main` no longer exposes raw runtime exception messages from the shared `Commands` reporters, intended validation UX remains explicit, focused regression source exists, and this claim is `COMPLETED` with exact integration evidence.
