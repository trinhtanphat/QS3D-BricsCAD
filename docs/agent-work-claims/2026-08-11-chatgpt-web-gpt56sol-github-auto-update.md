# Agent Work Claim — GitHub release auto-update

- Claim ID: `GITHUB-RELEASE-AUTO-UPDATE-20260811`
- Owner: `ChatGPT Web / GPT-5.6 Sol`
- Status: `ACTIVE`
- Start Time (UTC): `2026-08-11T13:46:30Z`
- Last Update (UTC): `2026-08-11T13:46:30Z`
- Baseline main SHA: `31c704f334a97723eae0ef966427931f81e9e94a`

## Scope

Implement a secure in-plugin update experience for QS3D on user machines. The loaded BricsCAD plugin will check published GitHub releases for `trinhtanphat/QS3D-BricsCAD`, compare release SemVer against the running informational version, expose a `QS3DUPDATE` Update Center with explicit **Check again** and **Update now** buttons, and notify the user when a newer release exists. Automatic installation must reuse the repository's existing signed update package contract and must not overwrite loaded DLLs in-process.

A signed release will publish `QS3D-BricsCAD-V25.update.json` alongside the ZIP/checksum. Clicking **Update now** schedules a detached updater that waits for BricsCAD to close normally, verifies the currently installed updater script's Authenticode signer against the running signed plugin, runs the existing hardened `update-v25.ps1`, and restarts the same BricsCAD executable after success. Unsigned builds/releases fail closed for one-click install and retain a manual release-page path.

## Allowed Paths

- `docs/agent-work-claims/2026-08-11-chatgpt-web-gpt56sol-github-auto-update.md`
- `docs/AUTO-UPDATE-PLAN-2026-08-11.md`
- `src/QS3D.BricsCAD.V25/Updates/**`
- `src/QS3D.BricsCAD.V25/PluginEntry.cs`
- `.github/workflows/release-v25.yml`
- `scripts/preflight-auto-update.py`
- `scripts/package-v25.ps1` only if source inspection proves the new command must be added to the packaged DemandLoad command manifest

## Forbidden Paths

- `src/QS3D.Core/**`
- `src/QS3D.BricsCAD.V25/Commands.cs`
- `src/QS3D.BricsCAD.V25/Ribbon/**`
- Start Center files currently owned by the active Start Center claim
- Quantity/BOQ/Workspace/Room/Family/modeless-viewer files owned by other active claims
- unrelated installers, signing scripts, release workflows, or preflight gates

## Dependencies / Assumptions

- GitHub Releases remain the public update source.
- Existing `update-v25.ps1`, `install-v25-autoload.ps1`, SHA-256 checks, archive safety checks, and Authenticode publisher pinning remain the canonical installation trust chain.
- Stable one-click auto-update requires signed release payloads. An unsigned currently installed preview has no trusted publisher anchor, so it may detect newer releases but must require one manual transition to a signed build before one-click updates are enabled.
- The release workflow remains manual-only; this claim does not dispatch Actions or publish a release.

## Overlap Policy

No edits outside Allowed Paths. Do not modify the active Start Center lane or current command/quantity/ribbon claims. Re-read `main` before every write; preserve concurrent agent commits; never force-update `main`.

## Environment / Host Needs

Source implementation is connector-safe. Real BricsCAD V25/WPF interaction, Windows Authenticode behavior, process-exit handoff, and restart behavior require the local Windows/BricsCAD environment for runtime qualification.

## Validation

- Add an auto-discovered static preflight covering GitHub endpoint pinning, SemVer comparison, signed-manifest gating, detached wait-for-BricsCAD-exit handoff, safe command registration, and release-workflow manifest upload.
- Re-fetch committed source and inspect current `main` ancestry after writes.
- Do not dispatch GitHub Actions unless the repository owner separately asks for CI/build/release execution.

## Release Conditions

Set this claim to `RELEASED` only after planning + implementation + source/preflight contract are committed to `main`, current HEAD is verified, and any native-only validation gap is stated explicitly without claiming remote runtime proof.