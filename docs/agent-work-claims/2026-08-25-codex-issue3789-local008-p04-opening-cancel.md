# LOCAL-008 P04 — Door/Opening per-prompt physical ESC qualification

- Agent: `codex`
- Date: 2026-08-25 (UTC+7)
- Status: `LOCAL_PASS / BOUNDED`
- Issue: #3789
- Parent: #74; queue parent #72
- Branch: `agent/codex/issue3789-local008-p04-opening-cancel`
- Baseline: `origin/main@a917e823799ebeaad2f629daecf9a611b6c0231d`
- Execution environment: licensed BricsCAD V25.2.10 on Windows x64

## Scope

Qualify the bounded cancel-before-execute lifecycle for production Door and
WallOpening Direct Draw commands on one exact pushed candidate:

- Quick `QS3DDRAWDOOR` / `QS3DDRAWOPENING`: physical ESC at the first and
  second geometry prompts;
- Advanced `QS3DDRAWDOORADV` / `QS3DDRAWOPENINGADV`: physical ESC at the
  first point, second point, height, sill and boolean-clearance prompts;
- every case returns to an idle editor and leaves no newly created/cached QS3D
  project, sidecar/backup/lock, command-owned source, semantic element,
  generated/native entity, implied selection or project audit/version change.

P01 view/consecutive/cancel and P03 repeated DrawJig V25/V26 are already PASS
and are not rerun. This lane does not claim project-identity drift, Auto Host
success/ambiguity, reference LINE, Ribbon/UI/DPI, save/reopen or broader #74.

## Boundary

This local lane may add only ignored runtime probe material and sanitized
claim/inbox evidence. It will not edit production source. A reproducible source
defect is handed to a distinct remote/source issue and the licensed cell waits
for a new exact pushed fix SHA.

Raw/proprietary runtime output stays under ignored `artifacts/`. DemandLoad,
installed-loader bytes, disposable fixtures and exact owned BricsCAD processes
must be restored/cleaned before a result is published.

## Coordination check

Current `origin/main`, issue #74, open Direct Draw issues/PRs and current
LOCAL-008 claims were inspected before registration. No open child or PR owns
this exact Door/Opening per-prompt physical-ESC no-residue matrix.

## Exact candidate and baseline

- Runtime-tested source SHA: `e945f0f88d56449443ef73be2e6f0cb35d0dd823`.
- Candidate state at launch: clean, committed and already pushed on
  `agent/codex/issue3789-local008-p04-opening-cancel`.
- Pinned `external/QS3D-Platform` SHA:
  `a5778f4abcf3b5c308c5d6854040dbc0c3082390`.
- Host: licensed BricsCAD V25.2.10, Windows x64, CLR 4, native host major 25.
- Exact adapter SHA-256:
  `D700CE91E526FFB75415ED065A359A16F1FD7F11DB3DC3A18559BF2621FF8AA0`.
- Exact Core SHA-256:
  `CC5515589803131F060B202DF202FDEF177E45E4099E5F32ECD770827CC05C44`.
- Repository fixture SHA-256:
  `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`.

The official baseline passed the manual-CI policy guard, generic local
preflight, all `1021/1021` aggregate feature gates, Core Release build with
zero warnings/errors, deterministic Core smoke (`ALL PASS`), V25 Release|x64
build with zero warnings/errors, offline WPF validation and licensed
exact-candidate NETLOAD/Ribbon/Palette/runtime identity. DemandLoad was isolated
from `LOADCTRLS=2` to `0` for exact NETLOAD and restored to `2`; the installed
loader SHA-256 remained
`0D89D8D828BCE5CFC966EC2EF54358DC50E4FED560D5A908F94643AFA1D74E30`.

## Result

`LOCAL_PASS / BOUNDED`: all 14 requested physical-ESC cases passed on the exact
candidate:

- Quick Door and WallOpening: first and second geometry prompts.
- Advanced Door and WallOpening: first point, second point, height, sill and
  clearance prompts.

Each case used a separate licensed BricsCAD process and disposable fixture.
The runner delivered physical ESC to the verified exact PID. Every production
command ended normally (`terminal=ENDED`, `failure_code=NONE`) and every
no-residue assertion passed: project cache/readability/pending state remained
absent; no `.qsdb`, backup or lock appeared; ModelSpace count and digest,
implied selection and `DBMOD` were unchanged. All 14 disposable DWGs remained
byte-identical and the repository fixture was untouched.

The production commands returned their result marker but did not close their
host process within the bounded post-result window. After evidence capture the
runner force-stopped only each verified owned PID (`forced_stop_count=14`).
DemandLoad and installed-loader bytes were restored and zero BricsCAD
processes remained. Therefore this cell makes no graceful-close,
save/reopen or cold-reopen claim.

## Excluded setup and pilot attempts

The following attempts produced no product verdict and are excluded from the
official result: no .NET SDK on `PATH`; portable SDK with the pinned submodule
not yet initialized; a DemandLoad run that selected the installed plugin before
the candidate; and the first single-case pilot whose PowerShell metadata
serialization failed after the product marker. The corrected second single-case
pilot passed but was validation-only. The official result is solely the clean
14-case matrix plus the official baseline described above.

## Scope closeout

No production source, test or tracked runner was changed. Raw markers and
private machine paths remain under ignored `artifacts/`. The post-test carrier
merged newer `origin/main@2c5e62a066829a82d930bd83233fd1028c30e5c1`;
that merge did not change the Direct Draw source or queue files relevant to
this P04 matrix, and the exact tested SHA remains an ancestor.

This closes only LOCAL-008/P04. Parent #74 and LOCAL-008 remain open for
project/context drift, Auto Host/reference and Ribbon/UI coverage.
