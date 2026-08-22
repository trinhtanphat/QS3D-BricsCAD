# Work claim — Runtime diagnostics display clarity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-13T17:12:00+07:00`
- Baseline main SHA: `96c6c960e29a1720790988f46cf55ccaca359a7d`
- Priority: Complete the owner-requested session audit by closing the previously unfinished `QS3DRUNTIMECHECK` display lane before the session handoff is finalized.

## Reserved scope

Make `QS3DRUNTIMECHECK` output unambiguous when no project state is available and structure long diagnostic values so the BricsCAD command line does not rely on dense, clip-prone single-line summaries. Preserve the existing runtime/version/stale-process detection semantics.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/RuntimeDiagnosticsCommands.cs`
- The V26 adapter only insofar as it shares/links the V25 source.
- A deterministic source guard/preflight covering the display contract.
- `scripts/preflight.py` only if registration is required for the new guard.
- This claim file for close-out.

## Excluded scope

- No changes to stale-DLL fingerprinting, version comparison, package metadata semantics, release workflow behavior, installer SemVer behavior, project persistence semantics, or local BricsCAD qualification.
- No GitHub Actions dispatch/rerun is part of this lane.
- No LOCAL_ONLY work is claimed.

## Validation plan

- Static regression must reject the legacy `Project: not loaded/persisted` wording.
- Static regression must require explicit loaded/persisted/read-only state wording and structured labels for long runtime identity fields.
- Confirm V25 source remains compatible with the V26 shared-source arrangement.
- Read back the pushed source and regression from current `main`.

## Coordination

No active claim visible at registration touched `RuntimeDiagnosticsCommands.cs`, `QS3DRUNTIMECHECK`, or this display-only contract. Recent runtime/version/stale-DLL fixes are treated as neighboring completed work and are explicitly preserved.

## Completion condition

The display fix and deterministic regression are pushed to `main`, read back successfully, and this claim is updated to `COMPLETED` with exact implementation/evidence SHAs and any remaining LOCAL_ONLY qualification boundary.
