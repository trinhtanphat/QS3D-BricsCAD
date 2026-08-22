# Work claim — Issue #1099 Update/version validation close-out

- Status: `COMPLETED`
- Agent: `codex-fix-updater-version-gates-20260814` (`/root/fix_updater_version_gates`)
- Registered: `2026-08-14T09:13:34+07:00`
- Completed: `2026-08-14T10:02:00+07:00`
- Baseline main SHA: `608d66195a2a532b73e5a85f326a876bd52ca1d6`
- Priority: close GitHub issue `#1099` with fresh full-checkout evidence after its source fix landed

## Reserved scope

Independently audit and validate the already-landed issue #1099 repair on current `main`: canonical single `QS3DVERSION` ownership, retained `QS3DVER` / `QSVER` aliases, current Update/manual-preview/security copy, and Windows legacy-console-safe gate failures. Run the installed-reference V25 `Release|x64` build, the eight issue-named focused gates, the current broader Update gate affected by canonical command ownership, and aggregate `preflight-all.py`. If those checks expose a concrete regression, make only the smallest evidence-supported source/gate correction before repeating the same validation.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/RuntimeDiagnosticsCommands.cs`
- `src/QS3D.BricsCAD.V25/Updates/UpdateCommands.cs`
- `scripts/preflight-update-manifest-preclose.py`
- `scripts/preflight-update-manual-preview-channel.py`
- `scripts/preflight-v25-netload-update-ux.py`
- `scripts/preflight-auto-update.py`
- duplicate-command gates named in GitHub issue `#1099`
- this claim and GitHub issue/PR close-out metadata

## Excluded scope

- LOCAL-002/P10 Curtain source, probe, runner, claim, inbox, evidence, or private/runtime data.
- BricsCAD interactive runtime, NETLOAD/DemandLoad, installer execution, release/signing, or any private fixture.
- GitHub Actions operation or workflow changes.
- Broad updater/security behavior changes, unrelated commands, or other agents' active lanes.

## Validation plan

- Build `src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj` as installed-reference `Release|x64`; require 0 warnings and 0 errors.
- Run the five duplicate-command gates and three Update UX gates listed in #1099, plus `scripts/preflight-auto-update.py`.
- Exercise legacy stdout encoding explicitly so Vietnamese assertion output cannot be hidden by `UnicodeEncodeError`.
- Run deterministic Core smoke if required by the aggregate, then `scripts/preflight-all.py`.
- Re-fetch/rebase before publish; review an exact, request-scoped diff; do not operate Actions.

## Coordination

This is the fresh-validator successor to `docs/agent-work-claims/2026-08-14-0852-chatgpt-web-gpt56sol-issue-1099-update-gates.md`, whose source fix `d92c7eb404b4c5dfeb8aee039905f83ad30bbce5` is on `main` but remained `PENDING_FRESH_VALIDATION`. Current owner commits `b4059961315ba7e6b5455ea8d41af65fe3c23227` and `f3b9bf99bbcfd566586dcbfa496fede3346816ee` further refine the canonical concise version command and its Update gate; they are included in the validation baseline and will not be reverted. No current `ACTIVE` / `BLOCKED` claim reserves these exact issue #1099 validation surfaces.

## Completion condition

Current-main descendant passes the installed-reference V25 build, every issue-named focused gate, the affected broader Update gate, explicit legacy-console output check, and aggregate preflight; any concrete regression is corrected and revalidated; this claim records exact SHAs/results; the coherent close-out is merged; and issue #1099 is closed without touching excluded lanes or GitHub Actions.

## Validation checkpoint — 2026-08-14 09:27 UTC+7

Fresh full-checkout validation found one issue-scoped stale gate and no remaining production-source defect:

- installed-reference V25 `Release|x64` build at `77ebd673a9f81ca3628e75328319427fa298a33f`: PASS, 0 warnings / 0 errors; product version `0.1.0-preview.7+77ebd673a9f81ca3628e75328319427fa298a33f`;
- all five duplicate-command gates from #1099: PASS;
- manifest-preclose, manual-preview and broader auto-update gates: PASS;
- `scripts/preflight-v25-netload-update-ux.py`: initially failed because it still required the superseded `VersionCheck()` → `RuntimeCheck()` forwarding and the pre-Build-identity single-line Update Center text;
- issue-scoped correction: require the current concise `WriteVersionSummary()` path, retain the separate `QS3DRUNTIMECHECK` deep-check handoff, and lock both loaded-DLL UI branches plus the full product-version tooltip;
- all nine focused issue/Update gates after that correction: PASS;
- explicit `PYTHONIOENCODING=cp1252` probes for all three affected `console_safe(...)` helpers emitted escaped Vietnamese assertion text and exited normally, with no `UnicodeEncodeError`;
- aggregate `scripts/preflight-all.py`: executed, 781 gates discovered, but not yet green because three unrelated current-main gates fail on research-document wording and Wall Junction signature drift (`preflight-product-boundary.py`, `preflight-research-implementation-status.py`, `preflight-wall-junctions.py`). Those surfaces are outside this reservation and were not edited.

At this checkpoint the #1099 gate correction was ready to publish, while final close-out and issue closure still waited for a fresh green aggregate result on a current-main descendant. No GitHub Actions operation was performed.

## Final close-out — exact main `907a2e9fa2c0c1282c8361ee8f02b49e7b2687ae`

- Claim-only commit `74d339a31d9d61f62afa73d5ae8c7412ef1be22e` merged through PR #1104 at `77ebd673a9f81ca3628e75328319427fa298a33f`.
- The original #1099 source/gate repair is `d92c7eb404b4c5dfeb8aee039905f83ad30bbce5`; later owner refinements `b4059961315ba7e6b5455ea8d41af65fe3c23227` and `f3b9bf99bbcfd566586dcbfa496fede3346816ee` preserve one concise canonical `QS3DVERSION` and the separate deep `QS3DRUNTIMECHECK` path.
- Focused gate correction `e7c8416e6de2248f85bdbf836447252b1cbc94e8` merged through PR #1110 at `a33240db617541f425163936095d8b136d3b6ad2`.
- Installed-reference V25 `Release|x64` build: PASS, 0 warnings / 0 errors; exact product version `0.1.0-preview.7+907a2e9fa2c0c1282c8361ee8f02b49e7b2687ae`; DLL SHA-256 `A5768B3FB01316FBFE3D0D97459BC737886AB31045418D5BFA406B9011B824B4`.
- Five duplicate-command gates, the three issue-named Update gates, and broader `preflight-auto-update.py`: all PASS.
- Explicit cp1252 probes for `preflight-v25-netload-update-ux.py`, `preflight-update-manifest-preclose.py`, and `preflight-update-manual-preview-channel.py`: PASS; Vietnamese assertion text was escaped and no `UnicodeEncodeError` occurred.
- Aggregate `scripts/preflight-all.py`: PASS, all 783 discovered feature gates passed.
- Production source retains exactly one canonical `QS3DVERSION` registration in `RuntimeDiagnosticsCommands`; `UpdateCommands` retains `QS3DVER` / `QSVER` through `WriteVersionCore` without re-registering the canonical command. The signed-manifest, manual-preview and updater security boundaries remain intact.
- No BricsCAD interactive/private-data operation and no GitHub Actions operation was performed.

This completion record is the close-out commit for the reserved lane. GitHub issue #1099 may be closed after this commit is merged to `main` and read back there.
