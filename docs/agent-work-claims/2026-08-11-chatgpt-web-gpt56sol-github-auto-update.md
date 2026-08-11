# Agent Work Claim — GitHub release auto-update

- Claim ID: `GITHUB-RELEASE-AUTO-UPDATE-20260811`
- Owner: `ChatGPT Web / GPT-5.6 Sol`
- Status: `RELEASED`
- Start Time (UTC): `2026-08-11T13:46:30Z`
- Last Update (UTC): `2026-08-11T14:10:30Z`
- Baseline main SHA: `31c704f334a97723eae0ef966427931f81e9e94a`

## Scope

Implement a secure in-plugin update experience for QS3D on user machines. The loaded BricsCAD plugin checks published GitHub releases for `trinhtanphat/QS3D-BricsCAD`, compares release SemVer against the running informational version, exposes a `QS3DUPDATE` Update Center with explicit **Kiểm tra lại** and **Cập nhật ngay** buttons, and notifies the user when a newer release exists. Automatic installation reuses the repository's existing signed update package contract and never overwrites loaded DLLs in-process.

A signed release publishes `QS3D-BricsCAD-V25.update.json` alongside the ZIP/checksum. Clicking **Cập nhật ngay** performs a fresh release check, schedules a detached updater, requests a graceful BricsCAD main-window close, waits for every BricsCAD process to exit, verifies the installed updater script's Authenticode signer against the running signed plugin, runs the existing hardened `update-v25.ps1`, and restarts the same BricsCAD executable only after success. BricsCAD retains normal unsaved-document save/cancel handling; cancelled shutdown is never escalated to a process kill. Unsigned builds/releases fail closed for one-click install and retain a manual release-page path.

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
- Start Center files owned by another lane during implementation
- Quantity/BOQ/Workspace/Room/Family/modeless-viewer files owned by other active claims
- unrelated installers, signing scripts, release workflows, or preflight gates

## Dependencies / Assumptions

- GitHub Releases remain the public update source.
- Existing `update-v25.ps1`, `install-v25-autoload.ps1`, SHA-256 checks, archive safety checks, and Authenticode publisher pinning remain the canonical installation trust chain.
- Stable one-click auto-update requires signed release payloads. An unsigned currently installed preview has no trusted publisher anchor, so it may detect newer releases but requires one manual transition to a signed build before one-click updates are enabled.
- The release workflow remains manual-only; this lane did not dispatch Actions or publish a release.

## Overlap Policy

Implementation stayed inside the declared Allowed Paths. `Commands.cs`, Ribbon, Start Center, Core, Quantity, Workspace, Room and Family lanes were not modified by this claim. Concurrent `main` commits were preserved; no reset, rebase or force update was used.

## Environment / Host Needs

Source implementation is complete. Real BricsCAD V25/WPF interaction, Windows Authenticode trust/timestamp behavior, graceful save/cancel shutdown, detached process-exit handoff, atomic upgrade/rollback and restart remain native/local qualification work. The repository already has the matching `LOCAL-009 — clean-machine install/sign/update qualification` item (`P1`, `OPEN`, `PENDING_LOCAL / DO_NOT_RETRY_REMOTE`); this claim intentionally does not create a duplicate LOCAL_ONLY queue item.

## Validation

- Added auto-discovered `scripts/preflight-auto-update.py` guarding fixed GitHub endpoint/repository identity, strict SemVer/channel rules, signed-manifest gating, current-publisher trust anchor, detached wait-for-BricsCAD-exit, graceful `CloseMainWindow()` request, explicit no-kill policy, updater-signer pinning, same-AssemblyVersion prerelease handoff, command/UI/lifecycle wiring, and signed-release manifest publication.
- Re-fetched the committed updater/release files from current `main` after concurrent agent activity.
- Ancestry compare from implementation checkpoint `fca6df0da82ffc1cdcffca25e54a706e03eadcdc` to then-current `main` reported `ahead`, `behind_by: 0`; subsequent agent changes were on non-updater paths.
- This connector session could not execute the repository checkout/native BricsCAD V25 qualification locally, so no runtime/build PASS is claimed here. The regression gate is committed for the repository/local runner.
- GitHub Actions were not dispatched and no GitHub Release was published by this lane, per repository manual-only CI/release policy.

## Completion Evidence

### Registration / planning

- `10e9e40b4a34ba4522ee68fc8d97e4989fcd0b41` — `chore(agent): claim GitHub auto-update lane`
- `15613ad6854189406bb3f4a6ea8cfa29eff333ca` — `docs(updater): plan secure GitHub release auto-update`
- `fca6df0da82ffc1cdcffca25e54a706e03eadcdc` — `docs(updater): align plan with graceful one-click close`

### Updater implementation

- `d3e65a03c3a7d4dce1d115971aecb4048d2214a8` — strict release SemVer ordering
- `d205a11246d47e8bd7d88f26e5e346b9b57c47fe` — bounded GitHub Releases client
- `0c2ad71443bcfed17023e9a28a109203ae9f835a` — signed detached updater handoff
- `e91bf5a8e57a62ac3eba01453c0df5b252c1e037` — update coordinator/state machine
- `b7f42bb708a0528e353a5114eab85165215c391c` — modeless Update Center UI
- `6ca7e61ab48abf156d2dd66e34c1ae40dc6c759e` — automatic release check/notification bootstrap
- `d9c395501cb73ee090bdc804d8acad2ede45d4c9` — `QS3DUPDATE` command
- `b2d7f66ba748de240839de835968a6aa8a5fd1cf` — plugin lifecycle wiring
- `4df9368d31a93a5d64c4bde6065b5028ac7b4fc0` — newer prerelease same-AssemblyVersion support
- `36f2ebfe54f0481288a0c123540f1ff24c949bd5` — GitHub/tag prerelease consistency gate
- `66c2ccc0c685f6ebc1acbfcf1dcfce5ade30516d` — graceful BricsCAD close request
- `bb10b6f427196f87ad1c6bd110e70e8f274367cb` — one-click UI graceful-close wiring
- `64607c4e17d95872b55fc4f6d0e1adc70f4aa198` — graceful handoff UX state

### Release/update contract

- `e7180920138357a1e7deebb57f031ff1bd77be1d` — signed release manifest generation/upload/publication gate

### Regression gate

- `12159590d88afd2127f49404d254184883e4f0b5` — initial secure auto-update source guard
- `6683b1b4dffc1edb81035badcc99b00923ce98bd` — prerelease/same-version guard
- `9716c70ff0160587b6589ca19180ae144ee33480` — graceful host-close/no-kill guard

## Release Conditions

Source lane conditions are satisfied: claim and planning were committed before implementation, updater/release/preflight source is on `main`, current ancestry was verified without overwriting concurrent work, and the remaining native-only evidence is explicitly delegated to existing `LOCAL-009`. Claim released at `2026-08-11T14:10:30Z`.