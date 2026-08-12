# Work claim — Generated Geometry Health error redaction

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-generated-geometry-health-error-redaction-20260812-1024`
- Registered: `2026-08-12T10:24:00+07:00`
- Baseline main SHA: `18d29069348f0808b3b3a24ae7236c08d63c1a9b`
- Priority: owner-requested continue-all residual diagnostic privacy hardening

## Confirmed defect

`src/QS3D.BricsCAD.V25/GeneratedGeometryHealthCommands.cs` currently catches `System.Exception ex` and reports `"QS3DGENERATEDHEALTH lỗi: " + ex.Message` through the shared `Report(...)` helper. `Report(...)` writes the same message to both `PaletteCoordinator.SetStatus(...)` and `Editor.WriteMessage(...)`, so filesystem/provider/environment details carried by an exception message can be reflected directly into user-visible diagnostics. This is inconsistent with the repository's established redaction contract for health/diagnostic failures.

## Reserved scope

- Redact raw exception-message reflection from the `QS3DGENERATEDHEALTH` top-level catch.
- Preserve command registration, read-only project access, stale-issue enumeration, issue-level diagnostics, summary behavior, Palette status reporting, and Editor reporting.
- Add one focused static regression preflight that rejects `ex.Message` in this command and pins the redacted failure message plus both report sinks.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/GeneratedGeometryHealthCommands.cs`
- `scripts/preflight-generated-geometry-health-error-redaction.py`
- this claim file

## Excluded scope

- No change to `GeneratedGeometryStaleHealthService` behavior or issue messages.
- No sibling `QS3DOWNERSHIPHEALTH` changes in this claim.
- No project mutation, persistence, generated-geometry rebuild, GitHub Actions dispatch, release publication, force push, or licensed BricsCAD V25/V26 runtime PASS claim.

## Validation plan

- Re-fetch current `main` source after claim registration before editing.
- Replace the raw exception-message catch with a stable generic failure message while preserving `Report(...)`.
- Add a focused Python source preflight for command registration, absence of `ex.Message`, stable generic failure text, and both Palette/Editor report sinks.
- Re-fetch source/preflight from current `main`, verify commit ancestry/readback, then close this claim with exact SHAs.

## Completion condition

Completed only when current `main` no longer reflects `ex.Message` from `QS3DGENERATEDHEALTH`, both user-visible report sinks remain intact, focused regression source pins the contract, and this claim is `COMPLETED` with exact integration evidence.