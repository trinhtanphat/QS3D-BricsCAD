# LOCAL-001 warm-cache sidecar revision runtime claim

Status: COMPLETED
Agent: codex-local003-sidecar-revision-20260815
Issue: #1574
Branch: `agent/local003/local001-sidecar-revision-20260815`
Baseline: `7b747ee26cb10b94ffad584dc256eaa19c6b65c8`
Instrumentation expansion baseline: `079e0e760cc0eac8704909ab042228641c703f4d`

## Reserved scope

- Qualify the existing LOCAL-001 licensed BricsCAD V25 warm-cache sidecar revision matrix through `scripts/test-bricscad-v25-sidecar-revision.ps1`.
- Exercise backup appearance, primary byte replacement, and primary removal after cache warm-up.
- Prove fail-closed behavior at the read, bind, existing-mutation, Interchange-confirmation, and Save authority boundaries.
- Prove semantic state and DWG bytes remain unchanged on refusal and that the original sidecar bytes can be restored in the same canonical session.
- Publish only sanitized exact-SHA evidence, the minimum LOCAL_ONLY runner/probe correction if native V25 behavior makes one necessary, and the matching canonical LOCAL-001 handoff/status update.
- If the native command returns only the existing generic failure token, add fixed privacy-safe stage tokens to the automation-only probe so the local run can identify the failing qualification boundary without exposing exception text, paths, project identity, drawing identity, handles, or semantic payload.

Expected repository surfaces are limited to:

- `docs/agent-work-claims/2026-08-15-codex-local001-sidecar-revision-runtime.md`
- `scripts/test-bricscad-v25-sidecar-revision.ps1` only if a genuinely local-dependent harness defect is reproduced
- `src/QS3D.BricsCAD.V25/SidecarRevisionProbeCommands.cs` only for fixed-token automation diagnostics or another minimum local-dependent probe correction
- focused static guards for that runner only if the same harness correction requires them
- `docs/LOCAL-AGENT-INBOX.md` and an existing current/local handoff document only for sanitized exact-SHA results

Generated licensed-runtime evidence remains under ignored `artifacts/` or an external local evidence directory.

## Exclusions

- No production persistence, sidecar, Interchange, Save, user-facing adapter, or Core source changes; the only allowed adapter edit is the automation-only probe command named above.
- No general bug fixing, broad source audit, unrelated documentation cleanup, private DWG/assets, proprietary binaries, secrets, screenshots, or raw machine evidence.
- Any ordinary source defect discovered by this qualification is handed to a non-local agent with the smallest sanitized reproduction; this local worker stops at that bug boundary.
- No GitHub Actions dispatch/re-run/cancel, release, direct `main` commit, force-push, or merge to `main`.
- This bounded matrix does not qualify the full LOCAL-001 surface or customer release.

## Validation plan

1. Fetch and integrate current `origin/main`, then verify this claim commit is pushed and visible on the task branch/draft PR before qualification.
2. Use one clean committed and pushed exact SHA for every static gate, build, and licensed runtime run.
3. Run the focused sidecar revision preflights, PowerShell AST parse, strict manual-CI/handoff guards, full Core smoke, and the V25 x64 Release build with the repository's portable .NET SDK workflow.
4. Start from zero BricsCAD processes and run the warm-cache sidecar revision matrix against a disposable copy of the canonical sample fixture and a fresh external evidence root.
5. Verify exact process cleanup, fixture and source-worktree cleanliness, no committed/generated private artifacts, and restore original sidecar bytes in the canonical session.
6. Publish sanitized result documentation and issue/PR evidence; leave the PR draft and stop before merge.

The 2026-08-15 first exact-SHA attempt returned only `SIDECAR_REVISION_PROBE_FAILED` and the host was already zero-process after runner cleanup. The instrumentation expansion is therefore reserved before touching the automation-only command; it is diagnostic evidence work, not authority to absorb a production source defect.

## Completion evidence

- Exact tested source and pushed branch SHA: `cfc80fe80f1bf866fdec27111eb5fdf1977a3305`.
- BricsCAD V25.2.10 x64; plugin/Core ProductVersion `0.1.0-preview.10040+cfc80fe80f1bf866fdec27111eb5fdf1977a3305`.
- Adapter SHA-256 `E06C477F04DC18546D89B8DC0C291783D4FFDF82820F2E8D0F68D8EC9C68CA68`; Core SHA-256 `6F7220CF5318A1E70A1F09B59E449E64A16FF744A4DAF82A35CECB66CAFF0685`.
- Focused sidecar/Save/static/manual-CI/handoff gates and PowerShell AST passed; full Core smoke reported `ALL PASS`; installed-reference V25 `Release|x64` build completed with zero warnings and zero errors.
- Licensed marker reported PASS for backup appearance, primary replacement and primary removal; read, bind, existing mutation, Interchange confirmation and Save all refused stale authority; project state was unchanged and same-session byte restoration recovered canonical authority.
- Fixture and disposable drawing hashes remained `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`.
- Final inventory contained only the synthetic drawing copy plus sanitized marker/metadata. BricsCAD process count was zero; probe environment, script, QSDB, backup, project lock and drawing lock residue were absent; the Git worktree remained clean.
- GitHub Actions, release and merge were not operated. Draft PR #1577 remains the owner-review boundary. This completes only the claimed warm-cache revision row, not overall LOCAL-001 or customer-release qualification.
